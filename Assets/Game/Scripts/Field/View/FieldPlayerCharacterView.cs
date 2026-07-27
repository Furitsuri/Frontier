using Frontier;
using Frontier.Entities;
using System;
using System.Collections;
using UnityEngine;
using Zenject;

namespace Frontier.Field
{
    /// <summary>
    /// フィールド上で自身を表す3Dキャラクターモデルのビュー。
    /// UserDomain.Members[0] のステータス(CHARACTER_TAG・PrefabIndex)から
    /// CharacterFactory 経由でモデルを一度だけ生成し、ノード間の移動に追従させる。
    /// </summary>
    public class FieldPlayerCharacterView : MonoBehaviour
    {
        [SerializeField] private float     _moveSpeed           = 3f;
        [SerializeField] private float     _modelScale          = 1f;
        [SerializeField] private Direction _facingDirection     = Direction.BACK;    // BACK = Z軸負方向。カメラ(画面正面)側を向く
        [SerializeField] private float     _cameraForwardOffsetZ = -0.5f;            // ノードのZ位置よりカメラ側にずらし、ノード描画より手前に表示する

        [Inject] private CharacterFactory _characterFactory = null;

        private Character _character     = null;
        private Coroutine  _moveCoroutine = null;

        /// <summary>
        /// 指定ノード位置にモデルを配置します(未生成の場合はここで生成します)。
        /// </summary>
        public void Setup( Vector3 nodeWorldPos )
        {
            EnsureCharacterCreated();
            transform.position = ApplyForwardOffset( nodeWorldPos );
        }

        /// <summary>
        /// ノード間を移動します。完了後 onComplete を呼びます。
        /// </summary>
        public void MoveTo( Vector3 targetNodeWorldPos, Action onComplete = null )
        {
            EnsureCharacterCreated();

            var target = ApplyForwardOffset( targetNodeWorldPos );
            if ( _moveCoroutine != null ) StopCoroutine( _moveCoroutine );
            _moveCoroutine = StartCoroutine( MoveRoutine( target, onComplete ) );
        }

        /// <summary>
        /// UserDomain.Members[0] のステータスから3Dモデルを一度だけ生成します。
        /// </summary>
        private void EnsureCharacterCreated()
        {
            if ( _character != null ) return;

            if ( _characterFactory == null )
            {
                Debug.LogWarning( "[FieldPlayerCharacterView] CharacterFactory が注入されていません。" );
                return;
            }

            var members = GameSession.Instance?.UserDomain?.Members;
            if ( members == null || members.Count == 0 )
            {
                Debug.LogWarning( "[FieldPlayerCharacterView] UserDomain.Members が空のため、フィールド上のキャラクターモデルを生成できません。" );
                return;
            }

            // アニメーション再生に必要なハッシュテーブルは通常 GameMain 起動時に初期化されるが、
            // FieldScene を単体で起動した場合(デバッグ実行等)は未初期化のためここで保証する
            if ( AnimDatas.AnimNameHashList == null )
            {
                AnimDatas.Init();
            }

            var status = members[0];
            _character = _characterFactory.CreateCharacter( status.characterTag, status );
            _character.OnFieldEnter();

            var characterTransform = _character.transform;
            characterTransform.SetParent( transform, false );
            characterTransform.localPosition = Vector3.zero;
            characterTransform.localScale    = Vector3.one * _modelScale;
            _character.SetRotation( _facingDirection );

            // アニメーションは一旦常時待機状態にする
            _character.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.WAIT );
        }

        private Vector3 ApplyForwardOffset( Vector3 nodeWorldPos )
        {
            return new Vector3( nodeWorldPos.x, nodeWorldPos.y, nodeWorldPos.z + _cameraForwardOffsetZ );
        }

        private IEnumerator MoveRoutine( Vector3 target, Action onComplete )
        {
            while ( Vector3.Distance( transform.position, target ) > 0.01f )
            {
                transform.position = Vector3.MoveTowards( transform.position, target, _moveSpeed * Time.deltaTime );
                yield return null;
            }
            transform.position = target;
            _moveCoroutine = null;
            onComplete?.Invoke();
        }
    }
}
