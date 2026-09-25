using Frontier.Entities;
using Frontier.Stage;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Sequences
{
    /// <summary>
    /// 攻撃を寄りの演出で見せる攻撃シーケンスです。
    /// モザイクをかけながらカメラを近づけ、攻撃者と被攻撃者のみを専用の戦闘フィールドに配置して攻撃を行わせます。
    /// 攻撃終了後はステージ上の元の位置へ戻します。
    /// </summary>
    public class CloseUpAttackSequence : CharacterAttackSequenceBase
    {
        [Inject] private StageController _stageCtrl = null;

        private BattleCameraController _btlCamCtrl = null;

        public CloseUpAttackSequence( Character attackChara, Character targetChara ) : base( attackChara, targetChara ) { }

        protected override COMBAT_ANIMATION_TYPE ClosedAnimationType => COMBAT_ANIMATION_TYPE.CLOSED;

        protected override void OnStart()
        {
            _btlCamCtrl = _btlRtnCtrl.GetBtlCameraCtrl;

            // 対象選択中のカメラズームを解除する(以降は攻撃シーケンス用カメラが制御するため、ズームは引き継がない)
            _btlCamCtrl.SetTargetSelectZoomActive( false );

            // 攻撃シーケンスの開始
            _btlCamCtrl.StartAttackSequenceMode( _attackCharacter, _targetCharacter );

            // 攻撃シーケンス用の演出中は、キャラクター頭上のHPゲージを一時的に非表示にする
            _uiSystem.BattleUi.SetHpGaugesActive( false );
        }

        protected override bool IsReadyToAttack( float rotateRate ) => _btlCamCtrl.IsFadeAttack();

        protected override void OnReadyToAttack()
        {
            TransitBattleField( _attackCharacter, _targetCharacter );
        }

        protected override void OnAttackTurnFinished()
        {
            // カメラ対象とカメラパラメータを変更
            _btlCamCtrl.TransitNextPhaseCameraParam( null, _targetCharacter.transform );
        }

        protected override void OnFinishAttack()
        {
            TransitStageField( _attackCharacter, _targetCharacter );  // バトルフィールドからステージフィールドに遷移

            // キャラクター頭上のHPゲージ表示を再開
            _uiSystem.BattleUi.SetHpGaugesActive( true );

            // 攻撃シーケンス用カメラを終了
            _btlCamCtrl.EndAttackSequenceMode( _attackCharacter );
        }

        protected override bool UpdateFinishing() => _btlCamCtrl.IsFadeEnd();

        /// <summary>
        /// 戦闘フィールドに遷移します
        /// </summary>
        /// <param name="attacker">攻撃キャラクター</param>
        /// <param name="target">被攻撃キャラクター</param>
        private void TransitBattleField( Character attacker, Character target )
        {
            foreach( var chara in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.PLAYER, CHARACTER_TAG.ENEMY ) )
            {
                if( chara != attacker && chara != target )
                {
                    chara.gameObject.SetActive( false );
                }
            }

            var centralPos = _stageCtrl.GetCentralPos(); // キャラクターをステージの中心位置からそれぞれ離れた場所に立たせる

            // 味方と敵対側で分別
            Character ally = null;
            Character opponent = null;
            if( attacker.GetStatusRef.IsMatchCharacterTag( CHARACTER_TAG.PLAYER ) )
            {
                ally = attacker;
                opponent = target;
            }
            else
            {
                if( target.GetStatusRef.IsMatchCharacterTag( CHARACTER_TAG.PLAYER ) )
                {
                    ally = target;
                    opponent = attacker;
                }
                else if( target.GetStatusRef.IsMatchCharacterTag( CHARACTER_TAG.OTHER ) )
                {
                    ally = target;
                    opponent = attacker;
                }
                else
                {
                    ally = attacker;
                    opponent = target;
                }
            }

            // 味方は奥行手前側、敵は奥行奥側の立ち位置とする
            Transform allyTransform = ally.transform;
            Transform opponentTransform = opponent.transform;
            allyTransform.position = centralPos + new Vector3( 0f, 0f, -COMBAT_POS_LENGTH_FROM_CENTER );
            opponentTransform.position = centralPos + new Vector3( 0f, 0f, COMBAT_POS_LENGTH_FROM_CENTER );
            allyTransform.rotation = Quaternion.LookRotation( centralPos - allyTransform.position );
            opponentTransform.rotation = Quaternion.LookRotation( centralPos - opponentTransform.position );
            // カメラパラメータを戦闘フィールド用に設定
            _btlCamCtrl.AdaptBattleFieldSetting();
        }

        /// <summary>
        /// ステージフィールドに遷移します
        /// </summary>
        /// <param name="attacker">攻撃キャラクター</param>
        /// <param name="target">被攻撃キャラクター</param>
        private void TransitStageField( Character attacker, Character target )
        {
            foreach( var chara in _btlRtnCtrl.BtlCharaCdr.GetCharacterEnumerable( CHARACTER_TAG.PLAYER, CHARACTER_TAG.ENEMY ) )
            {
                chara.gameObject.SetActive( true );
            }

            // キャラクターをステージの中心位置からそれぞれ離れた場所に立たせる
            var tileData = _stageCtrl.GetTileStaticData( attacker.BattleParams.TmpParam.CurrentTileIndex );
            _attackCharacter.transform.position = tileData.CharaStandPos;
            _attackCharacter.transform.rotation = _atkCharaInitialRot;
            tileData = _stageCtrl.GetTileStaticData( target.BattleParams.TmpParam.CurrentTileIndex );
            _targetCharacter.transform.position = tileData.CharaStandPos;
            _targetCharacter.transform.rotation = _tgtCharaInitialRot;
        }
    }
}
