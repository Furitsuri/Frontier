using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Arrow : Bullet
{
    override public IEnumerator UpdateTransformCoroutine(UnityAction callback)
    {
        _isHit = false;

        var diff    = _targetCoordinate - _firingPoint;
        var diff_XZ = new Vector3(diff.x, 0f, diff.z);

        // ����������
        Vector3 velocity = Vector3.zero;
        velocity.z  = diff_XZ.magnitude / _flightTime;
        velocity.y  = diff.y / _flightTime - 0.5f * Physics.gravity.y * _flightTime;
        
        for( float time = 0f; time < _flightTime; time += Time.deltaTime )
        {
            Vector3 nextPos = Vector3.Lerp(_firingPoint, _targetCoordinate, time / _flightTime);
            nextPos.y = _firingPoint.y + velocity.y * time + 0.5f * Physics.gravity.y * time * time;

            // ��̌��������݂̑��x���狁�߂�
            var currentVelocity = diff_XZ / _flightTime;
            currentVelocity.y   = velocity.y + Physics.gravity.y * time;
            currentVelocity.Normalize();
            // ��f���̃f�t�H���g�̌������A�V��Y���������ɂȂ��Ă���̂ň�xZ�����ʂ֌�������
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
