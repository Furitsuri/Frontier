using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Bullet : MonoBehaviour
{
    protected Vector3 _firingPoint;
    protected Vector3 _targetCoordinate;
    protected Vector3 _velocity;
    protected float _flightTime;
    protected bool _isHit;

    [SerializeField]
    protected float _flightTimePerGrid;

    void Awake()
    {
        OnAwake();
    }

    // Start is called before the first frame update
    void Start()
    {
        OnStart();
    }

    /// <summary>
    /// Awakeの際に呼ばれる関数です
    /// </summary>
    virtual protected void OnAwake()
    {
        _firingPoint        = Vector3.zero;
        _targetCoordinate   = Vector3.zero;
        _velocity           = Vector3.zero;
        _flightTime         = 0f;
        _isHit              = false;
    }

    /// <summary>
    /// Startの際に呼ばれる関数です
    /// </summary>
    virtual protected void OnStart()
    {
    }

    /// <summary>
    /// 位置更新用のコルーチンです
    /// BulletをActiveにした後に呼び出してください
    /// </summary>
    /// <returns></returns>
    virtual public IEnumerator UpdateTransformCoroutine( UnityAction<bool> callback )
    {
        yield return null;
    }

    public Coroutine StartUpdateCoroutine(UnityAction<bool> callback)
    {
        return StartCoroutine(UpdateTransformCoroutine(callback));
    }

    /// <summary>
    /// 発射座標を設定します
    /// </summary>
    /// <param name="point">発射座標</param>
    public void SetFiringPoint(in Vector3 point)
    {
        _firingPoint = point;
        transform.position = _firingPoint;
    }

    /// <summary>
    /// 射撃目標座標を決定します
    /// </summary>
    /// <param name="coordinate">目標座標</param>
    public void SetTargetCoordinate( in Vector3 coordinate )
    {
        _targetCoordinate = coordinate;
    }

    /// <summary>
    /// 射撃地点から目標地点までのグリッド長から弾の浮遊時間を設定します
    /// </summary>
    /// <param name="GridLength">グリッド長</param>
    public void SetFlightTimeFromGridLength( float GridLength )
    {
        _flightTime = _flightTimePerGrid * GridLength;
    }

    /// <summary>
    /// 弾が目標地点に達したかを取得します
    /// </summary>
    /// <returns>目標地点に達したか否か</returns>
    public bool IsHit() { return _isHit; }
}
