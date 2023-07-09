using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BattleCameraController : Singleton<BattleCameraController>
{
    public enum CameraMode
    {
        FOLLOWING = 0,

        NUM
    }

    private CameraMode _mode;
    private Camera _mainCamera;
    private Vector3 _lookAtPosition;
    private Vector3 _followingPosition;
    private Vector3 _offset;
    [SerializeField]
    private float _offsetLength;
    [SerializeField]
    private float _followDuration = 1f;
    private float _followElapsedTime = 0.0f;
    

    // Start is called before the first frame update
    override protected void OnStart()
    {
        _mainCamera         = Camera.main;
        _mode               = CameraMode.FOLLOWING;
        _lookAtPosition     = _mainCamera.transform.position + _mainCamera.transform.forward;
        _followingPosition  = _mainCamera.transform.position;
        _offset             = _followingPosition - _mainCamera.transform.forward;
        _offsetLength       = _offset.magnitude;
    }

    // Update is called once per frame
    override protected void OnUpdate()
    {
        switch( _mode )
        {
            case CameraMode.FOLLOWING:
                // MEMO : positionを決定してからLookAtを設定しないと、Unityの仕様なのか、画面におかしなかくつきが発生するため注意
                // _mainCamera.transform.position = _followingPosition;
                
                _followElapsedTime = Mathf.Clamp( _followElapsedTime + Time.deltaTime, 0f, _followDuration);
                _mainCamera.transform.position = Vector3.Lerp(_mainCamera.transform.position, _followingPosition, _followElapsedTime / _followDuration);

                // _mainCamera.transform.LookAt(_lookAtPosition);
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pos"></param>
    public void SetLookAtPosition( in Vector3 pos )
    {
        _lookAtPosition     = pos;
        _followingPosition  = _lookAtPosition + _offset;
        _followElapsedTime  = 0.0f;
        // _mainCamera.transform.position = _followingPosition;
        // _mainCamera.transform.LookAt(_lookAtPosition);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="length"></param>
    public void SetOffsetLength( float length )
    {   
        _offsetLength       = length;
        _offset             = _offset.normalized * _offsetLength;
        _followingPosition  = _lookAtPosition + _offset;
        _followElapsedTime  = 0.0f;
    }
}
