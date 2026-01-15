using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Arrow : Bullet
{
    public override IEnumerator UpdateTransformCoroutine(UnityAction callback)
    {
        _isHit = false;

        var diff    = _targetCoordinate - _firingPoint;
        var diffXZ  = diff.XZ();

        // 初速を決定
        Vector3 velocity = Vector3.zero;
        velocity.z  = diffXZ.magnitude / _flightTime;
        velocity.y  = diff.y / _flightTime - 0.5f * Physics.gravity.y * _flightTime;
        
        for( float time = 0f; time < _flightTime; time += DeltaTimeProvider.DeltaTime )
        {
            Vector3 nextPos = Vector3.Lerp(_firingPoint, _targetCoordinate, time / _flightTime);
            nextPos.y = _firingPoint.y + velocity.y * time + 0.5f * Physics.gravity.y * time * time;

            // 矢の向きを現在の速度から求める
            var currentVelocity = diffXZ / _flightTime;
            currentVelocity.y   = velocity.y + Physics.gravity.y * time;
            currentVelocity.Normalize();
            // 矢モデルのデフォルトの向きが、鏃をY軸下向きになっているので一度Z軸正面へ向かせる
            var nextRot         = Quaternion.AngleAxis(-90, Vector3.right);
            nextRot             = Quaternion.LookRotation(currentVelocity) * nextRot;

            transform.position = nextPos;
            transform.rotation = nextRot;
            yield return null;
        }

        transform.position = _targetCoordinate;
        gameObject.SetActive(false);
        _isHit = true;

        callback();
    }
}
