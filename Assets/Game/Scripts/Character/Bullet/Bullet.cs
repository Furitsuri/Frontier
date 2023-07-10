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
    /// Awake�̍ۂɌĂ΂��֐��ł�
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
    /// Start�̍ۂɌĂ΂��֐��ł�
    /// </summary>
    virtual protected void OnStart()
    {
    }

    /// <summary>
    /// �ʒu�X�V�p�̃R���[�`���ł�
    /// Bullet��Active�ɂ�����ɌĂяo���Ă�������
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
    /// ���ˍ��W��ݒ肵�܂�
    /// </summary>
    /// <param name="point">���ˍ��W</param>
    public void SetFiringPoint(in Vector3 point)
    {
        _firingPoint = point;
        transform.position = _firingPoint;
    }

    /// <summary>
    /// �ˌ��ڕW���W�����肵�܂�
    /// </summary>
    /// <param name="coordinate">�ڕW���W</param>
    public void SetTargetCoordinate( in Vector3 coordinate )
    {
        _targetCoordinate = coordinate;
    }

    /// <summary>
    /// �ˌ��n�_����ڕW�n�_�܂ł̃O���b�h������e�̕��V���Ԃ�ݒ肵�܂�
    /// </summary>
    /// <param name="GridLength">�O���b�h��</param>
    public void SetFlightTimeFromGridLength( float GridLength )
    {
        _flightTime = _flightTimePerGrid * GridLength;
    }

    /// <summary>
    /// �e���ڕW�n�_�ɒB���������擾���܂�
    /// </summary>
    /// <returns>�ڕW�n�_�ɒB�������ۂ�</returns>
    public bool IsHit() { return _isHit; }
}
