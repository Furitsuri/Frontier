using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Constants;

public class TransformHandler
{
    private Transform _transform;
    private Vector3 _velocity;                                  // 速度
    private Vector3 _accel;                                     // 加速度
    private Vector3 _prevPosition;                              // 1フレーム前の位置
    private Quaternion _prevRotation;                           // 1フレーム前の回転
    private Quaternion _orderdRotation = Quaternion.identity;   // 指示されている回転量

    public void Init( Transform transform )
    {
        _transform      = transform;
        _orderdRotation = Quaternion.identity;
        _velocity       = new Vector3( 0, 0, 0 );
        _accel          = new Vector3( 0, 0, 0 );
        _prevPosition   = _transform.position;
        _prevRotation   = _transform.rotation;
    }

    public void Update( float deltaTime )
    {
        // 前フレームの位置・回転を保存
        _prevPosition   = _transform.position;
        _prevRotation   = _transform.rotation;

        // 速度・位置を更新
        _transform.position += ( _velocity * deltaTime + 0.5f * _accel * deltaTime * deltaTime );
        _velocity += ( _accel * deltaTime );

        AdjustRotationToXZPlane();  // キャラクターの角度をXZ平面に垂直となるように補正

        // 向き回転命令
        if( _orderdRotation != Quaternion.identity )
        {
            _transform.rotation = Quaternion.Slerp( _transform.rotation, _orderdRotation, Constants.CHARACTER_ROT_SPEED * DeltaTimeProvider.DeltaTime );

            float angleDiff = Quaternion.Angle( _transform.rotation, _orderdRotation );
            if( Mathf.Abs( angleDiff ) < Constants.CHARACTER_ROT_THRESHOLD )
            {
                _orderdRotation = Quaternion.identity;
            }
        }
    }

    public void ResetVelocityAcceleration()
    {
        _velocity   = new Vector3( 0, 0, 0 );
        _accel      = new Vector3( 0, 0, 0 );
    }

    public void SetVelocityAcceleration( in Vector3 velocity, in Vector3 accel )
    {
        _velocity   = velocity;
        _accel      = accel;
    }

    public void SetPosition( in Vector3 position )
    {
        _transform.position = position;
    }

    public void SetPositionXZ( in Vector3 position )
    {
        _transform.position = new Vector3( position.x, _transform.position.y, position.z );
    }

    public void SetRotation( in Quaternion rotation )
    {
        _transform.rotation = rotation;
    }

    public void AddVelocityAcceleration( in Vector3 velocity, in Vector3 accel )
    {
        _velocity   += velocity;
        _accel      += accel;
    }

    public void AddPosition( in Vector3 position )
    {
        _transform.position += position;
    }

    public void AddRotation( in Quaternion rotation )
    {
        _transform.rotation *= rotation;
    }

    public Vector3 GetPosition()
    {
        return _transform.position;
    }

    public Vector3 GetPreviousPosition()
    {
        return _prevPosition;
    }

    public Quaternion GetRotation()
    {
        return _transform.rotation;
    }

    public Quaternion GetPreviousRotation()
    {
        return _prevRotation;
    }

    /// <summary>
    /// 指定インデックスのグリッドにキャラクターの向きを合わせるように命令を発行します
    /// </summary>
    /// <param name="targetPos">向きを合わせる位置</param>
    public void RotateToPosition( in Vector3 targetPos )
    {
        var directionXZ = ( targetPos - _transform.position ).XZ();
        _orderdRotation = Quaternion.LookRotation( directionXZ );
    }

    public void StartJump( in Vector3 departingPosition, in Vector3 destinationPosition, float moveSpeedRate )
    {
        var diffPosition    = destinationPosition - departingPosition;
        var diffPositionXZ  = diffPosition.XZ();
        float diffHeight    = diffPosition.y;
        float arrivalTime   = diffPositionXZ.magnitude / ( moveSpeedRate * Constants.CHARACTER_MOVE_SPEED );

        if( 0 < diffHeight )
        {
            // MEMO : 放物運動によって、一度最高点まで上昇し、そこから目的地まで落下する動作を実装する
            // 到達時刻TはXZ平面上の動きが等速であることから既に定められているため、ここでは初速度と落下加速度を求める

            // 1. 落下加速度gを求める( 基本公式 : h = v0 * t + 0.5 * g * t^2 → 0.5 * g * t^2 + v0 * t - h = 0 )
            //    解の和と積より、 t1 + t2 = - 2 * v0 / g, t1 * t2 = - 2 * h / g → t1 = - 2 * h / ( g * t2 ) より v0 = h / t2 - 0.5 * g * t2
            //    t1 < t2 であることが前提のため、上記 t1 = - 2 * h / ( g * t2 ) に t1 < t2 を代入したとき、
            //    - 2 * h / ( g * t2 ) < t2 → g < - 2 * h / t2^2 となる(gは負の値であることが分かっているため、等号は反対となる)
            //    よって、g = - 2 * h / t2^2 とし、そこに任意の負の値を加えることで、t1 < t2 を満たす g を求めることができる
            _accel.y = -2 * diffHeight / Mathf.Pow( arrivalTime, 2f );
            _accel.y -= JUMP_POSITIVE_Y_ACCELERATION; // t1 < t2 を満たすために、任意の負の値を加える

            // 2. 初速度v0を求める( 1における導出より v0 = h / t2 - 0.5 * g * t2 )
            _velocity.y = diffHeight / arrivalTime - 0.5f * _accel.y * arrivalTime;
        }
        else
        {
            // MEMO : 目的地が出発地点よりも低い場合は、単純に等加速度運動で落下する動作を実装する

            // h = v0 * t + 0.5 * g * t^2 が成り立つ
            // 0 < v0 が前提であり、それを満たす g を求める。v0には任意の正の値を代入する
            _velocity.y = JUMP_NEGATIVE_Y_VELOCITY;
            // 0.5 * g * t^2 + v0 * t - h = 0 より、 g = 2 * ( - v0 * t + h ) / t^2
            _accel.y = 2 * ( - _velocity.y * arrivalTime + diffHeight ) / ( arrivalTime * arrivalTime );
        }
    }

    /// <summary>
    /// キャラクターの角度をXZ平面に垂直となるように補正します
    /// </summary>
    private void AdjustRotationToXZPlane()
    {
        // キャラクターの向きを保ったまま、常にXZ平面に対して垂直にする
        Vector3 forward = _transform.forward.XZ();
        if( 0.0001f < forward.sqrMagnitude )
        {
            _transform.rotation = Quaternion.LookRotation( forward, Vector3.up );
        }
    }
}
