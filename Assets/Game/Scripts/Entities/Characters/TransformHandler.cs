using UnityEngine;
using static Constants;

public class TransformHandler
{
    private ReadOnlyReference<Transform> _readOnlyTransform;
    private Vector3 _velocity;                              // 速度
    private Vector3 _accel;                                 // 加速度
    private Vector3 _prevPosition;                          // 1フレーム前の位置
    private Quaternion _prevRotation;                       // 1フレーム前の回転
    private Quaternion _baseRotation;                       // ターン開始時や移動完了時など、回転命令が発行されていない状態の回転量(キャラクターの向き)を保存する(回転命令が発行されたときに、回転命令実行前の回転量を保存するために用いる)
    private Quaternion? _orderdRotation         = null;     // 指示されている回転量(nullは指示がないことを示す)

    public void Init()
    {
        _velocity       = new Vector3( 0, 0, 0 );
        _accel          = new Vector3( 0, 0, 0 );
        _prevPosition   = _readOnlyTransform.Value.position;
        _prevRotation   = _readOnlyTransform.Value.rotation;
        _orderdRotation = null;
    }

    public void Update( float deltaTime )
    {
        // 前フレームの位置・回転を保存
        _prevPosition   = _readOnlyTransform.Value.position;
        _prevRotation   = _readOnlyTransform.Value.rotation;

        // 速度・位置を更新
        _readOnlyTransform.Value.position += ( _velocity * deltaTime + 0.5f * _accel * deltaTime * deltaTime );
        _velocity += ( _accel * deltaTime );

        AdjustRotationToXZPlane();  // キャラクターの角度をXZ平面に垂直となるように補正

        // 向き回転命令
        if( _orderdRotation != null )
        {
            _readOnlyTransform.Value.rotation = Quaternion.Slerp( _readOnlyTransform.Value.rotation, _orderdRotation.Value, Constants.CHARACTER_ROT_SPEED * DeltaTimeProvider.DeltaTime );

            float angleDiff = Quaternion.Angle( _readOnlyTransform.Value.rotation, _orderdRotation.Value );
            if( Mathf.Abs( angleDiff ) < CHARACTER_ROT_THRESHOLD )
            {
                _orderdRotation = null;
            }
        }
    }

    public void Regist( Transform transform )
    {
        _readOnlyTransform = new ReadOnlyReference<Transform>( transform );
    }

    public void EstablishBaseRotation()
    {
        _baseRotation = _readOnlyTransform.Value.rotation;
    }

    public void SetVelocityAndAcceleration( in Vector3 velocity, in Vector3 accel )
    {
        _velocity   = velocity;
        _accel      = accel;
    }

    public void SetScale( float scale )
    {
        _readOnlyTransform.Value.localScale = Vector3.one * scale;
    }

    public void SetPosition( in Vector3 position )
    {
        _readOnlyTransform.Value.position = position;
    }

    public void SetPositionXZ( in Vector3 position )
    {
        _readOnlyTransform.Value.position = new Vector3( position.x, _readOnlyTransform.Value.position.y, position.z );
    }

    public void SetRotation( in Quaternion rotation )
    {
        _readOnlyTransform.Value.rotation = rotation;
    }

    /// <summary>
    /// Direction 値に対応した向き（Y軸 90°刻み）を即時設定します。
    /// FORWARD=0°, RIGHT=90°, BACK=180°, LEFT=270°
    /// </summary>
    public void SetRotation( Direction direction )
    {
        _readOnlyTransform.Value.rotation = Quaternion.AngleAxis( ( int ) direction * 90f, Vector3.up );
    }

    public void ResetVelocityAcceleration()
    {
        _velocity   = new Vector3( 0, 0, 0 );
        _accel      = new Vector3( 0, 0, 0 );
    }

    public void ResetRotationOrder()
    {
        _orderdRotation = _baseRotation;
    }

    /// <summary>
    /// 指定インデックスのグリッドにキャラクターの向きを合わせるように命令を発行します
    /// </summary>
    /// <param name="targetPos">向きを合わせる位置</param>
    public void RotateToPosition( in Vector3 targetPos )
    {
        var directionXZ = ( targetPos - _readOnlyTransform.Value.position ).XZ();
        OrderRotate( Quaternion.LookRotation( directionXZ ) );
    }

    public void AddVelocityAcceleration( in Vector3 velocity, in Vector3 accel )
    {
        _velocity   += velocity;
        _accel      += accel;
    }

    public void AddPosition( in Vector3 position )
    {
        _readOnlyTransform.Value.position += position;
    }

    public void AddRotation( in Quaternion rotation )
    {
        _readOnlyTransform.Value.rotation *= rotation;
    }

    public Vector3 GetPosition()
    {
        return _readOnlyTransform.Value.position;
    }

    public Vector3 GetPreviousPosition()
    {
        return _prevPosition;
    }

    public Quaternion GetRotation()
    {
        return _readOnlyTransform.Value.rotation;
    }

    public Quaternion GetPreviousRotation()
    {
        return _prevRotation;
    }

    public void OrderRotate( in Quaternion rotation )
    {
        _orderdRotation = rotation;
    }

    /// <summary>
    /// 速度・加速度の Y 成分のみを設定します。XZ 成分は維持されます。
    /// ジャンプ計算など、Y 方向の運動のみを上書きしたい場合に使用します。
    /// </summary>
    public void SetVerticalMotion( float velocityY, float accelY )
    {
        _velocity.y = velocityY;
        _accel.y    = accelY;
    }

    public Direction GetDirection()
    {
        Vector3 forward;

        // 向きへの指示値がある場合はその値を用いる
        if( null != _orderdRotation )
        {
            forward = _orderdRotation.Value * Vector3.forward;
        }
        // それ以外は現在の向きを用いる
        else
        {
            forward = _readOnlyTransform.Value.forward;
            forward.y = 0;
            forward.Normalize();
        }

        return Methods.ConvertDirectionFromVector( forward );
    }

    public Vector3 GetOrderedForward()
    {
        if( null != _orderdRotation )
        {
            return _orderdRotation.Value * Vector3.forward;
        }
        else
        {
            return _readOnlyTransform.Value.forward;
        }
    }

    /// <summary>
    /// キャラクターの角度をXZ平面に垂直となるように補正します
    /// </summary>
    private void AdjustRotationToXZPlane()
    {
        // キャラクターの向きを保ったまま、常にXZ平面に対して垂直にする
        Vector3 forward = _readOnlyTransform.Value.forward.XZ();
        if( 0.0001f < forward.sqrMagnitude )
        {
            _readOnlyTransform.Value.rotation = Quaternion.LookRotation( forward, Vector3.up );
        }
    }
}
