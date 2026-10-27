
using Frontier.Battle;
using Frontier.Entities;
using Frontier.Stage;
using Frontier.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Frontier.Combat
{
    public class JumpSlashSA : MovingSkillActionBase
    {
        /// <summary>
        /// ジャンプ斬りの範囲計算ルールを生成します(SkillRangeRules から参照されます)
        /// 高さ情報・使用者のジャンプ力が必要なため、StageController と使用者を閉じ込めたデリゲートを設定します
        /// </summary>
        public static SkillRangeRules CreateRangeRules( StageController stageCtrl, Character owner )
        {
            return new SkillRangeRules(
                reachability          : ( dprtIdx, prevIdx, tgtIdx, remainingRange ) => IsReachableByJump( dprtIdx, prevIdx, owner.GetStatusRef.jumpForce, stageCtrl ),
                postProcessor         : ( dprtIdx, atkRng, colNum, actionableTileData ) => FilterAttackTargets( dprtIdx, atkRng, colNum, owner.GetStatusRef.jumpForce, owner.GetCharacterKey(), actionableTileData, stageCtrl ),
                ghostResolver         : ( tileIndex, actionableTileData, ctrl, selfKey, landingRange ) => ResolveLandingTile( tileIndex, actionableTileData, ctrl, selfKey, landingRange, owner.GetStatusRef.jumpForce ),
                rangeAdjustmentFilter : IsKeepTileBeyondGhost,
                attackTargetFilter    : IsAttackTargetFromGhost,
                maxRangeResolver      : dprtIdx => CalcurateMaxLandingRange( dprtIdx, owner.GetStatusRef.jumpForce, stageCtrl ),
                targetableRangeMargin : 1 );    // 着地タイルの1つ先の相手を攻撃するため
        }

        // ゴースト距離より遠いタイルの保持・除外を判定するスキル固有フィルタ
        // ゴーストの直後 1 マスかつ攻撃対象フラグがある場合のみ保持する
        private static bool IsKeepTileBeyondGhost( int candidateIdx, int ghostRange, int candidateDist, TileDynamicData candidateTileData )
        {
            return candidateDist == ghostRange + 1 &&
                Methods.HasAnyFlag( candidateTileData.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST );
        }

        // 着地タイルの直後 1 マスの相手のみを攻撃対象とする
        // (着地タイルより手前の相手は飛び越えるだけで攻撃しない)
        private static bool IsAttackTargetFromGhost( int candidateIdx, int ghostRange, int candidateDist, TileDynamicData candidateTileData )
        {
            return candidateDist == ghostRange + 1;
        }

        /// <summary>
        /// 着地可能な最大の横方向の距離を求めます。
        /// 高さ h のタイルへは (自身のタイルの高さ - h) + ジャンプ力 の距離まで着地出来るため、ステージ上で最も低いタイルを基準とします。
        /// </summary>
        private static int CalcurateMaxLandingRange( int dprtIdx, int jumpForce, StageController stageCtrl )
        {
            ( int rowNum, int colNum ) = stageCtrl.GetGridNumsXZ();
            float minHeight = float.MaxValue;
            for( int i = 0; i < rowNum * colNum; ++i )
            {
                minHeight = Mathf.Min( minHeight, stageCtrl.GetTileStaticData( i ).Height );
            }

            float selfHeight = stageCtrl.GetTileStaticData( dprtIdx ).Height;
            return Mathf.Max( 0, Mathf.FloorToInt( selfHeight + jumpForce - minHeight ) );
        }

        /// <summary>
        /// 範囲を直前のタイルから次のタイルへ伸ばせるかを判定します。
        /// 直前のタイルを飛び越えられる場合のみ先へ伸ばします(飛び越えられないタイル自体は、着地タイルの先の攻撃対象となり得るため範囲に含めます)
        /// </summary>
        private static bool IsReachableByJump( int dprtIdx, int prevIdx, int jumpForce, StageController stageCtrl )
        {
            if( prevIdx == dprtIdx ) { return true; }

            int   prevDist   = stageCtrl.CalculateTotalRange( dprtIdx, prevIdx );
            float selfHeight = stageCtrl.GetTileStaticData( dprtIdx ).Height;
            return !IsBlockingJump( selfHeight, jumpForce, prevDist, prevIdx, stageCtrl );
        }

        /// <summary>
        /// 指定のタイルがジャンプの妨げとなる(飛び越えられない)かを判定します。
        /// 判定条件: n + JumpForce - x < h で飛び越え不可 (n=自身のタイルの高さ, x=横方向の距離, h=対象タイルの高さ)
        /// 対象タイルにキャラクターが存在する場合は、h にキャラクターの高さを加えます。
        /// </summary>
        private static bool IsBlockingJump( float selfHeight, int jumpForce, int dist, int tileIdx, StageController stageCtrl )
        {
            float obstacleHeight = stageCtrl.GetTileStaticData( tileIdx ).Height;
            if( stageCtrl.GetTileDynamicData( tileIdx ).CharaKey.IsValid() )
            {
                obstacleHeight += Constants.JUMP_SLASH_CHARACTER_HEIGHT;
            }

            return selfHeight + jumpForce - dist < obstacleHeight;
        }

        private static void FilterAttackTargets( int dprtIdx, int atkRng, int colNum, int jumpForce, CharacterKey selfKey, ActionableTileData actionableTileData, StageController stageCtrl )
        {
            int[] dirOffsets = { colNum, 1, -colNum, -1 };
            foreach( int dirOffset in dirOffsets )
            {
                FilterAttackTargetsInDirection( dprtIdx, atkRng, dirOffset, jumpForce, selfKey, actionableTileData, stageCtrl );
            }
        }

        // 指定の1方向について走査し、いずれかの着地タイルの直後にいて、かつ高さ条件を満たす敵のみを攻撃対象として残す
        private static void FilterAttackTargetsInDirection( int dprtIdx, int atkRng, int dirOffset, int jumpForce, CharacterKey selfKey, ActionableTileData actionableTileData, StageController stageCtrl )
        {
            var attackableMap = actionableTileData.AttackableTileMap;

            // 方向内の全敵タイルを収集しつつ ATTACKABLE_TARGET_EXIST フラグを一旦解除する
            var allEnemyIndices = new HashSet<int>();
            for( int l = 1; l <= atkRng; ++l )
            {
                int tileIdx = dprtIdx + l * dirOffset;
                if( !attackableMap.TryGetValue( tileIdx, out var tileData ) ) { break; }

                if( Methods.HasAnyFlag( tileData.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST ) )
                {
                    allEnemyIndices.Add( tileIdx );
                    Methods.UnsetBitFlag( ref tileData.Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST );
                }
            }

            // 着地先は選択可能なため、着地可能な全てのタイルについて、その直後にいる敵のフラグを復元する
            var landingTileIndices = ScanLandingTiles( dprtIdx, dirOffset, atkRng, jumpForce, attackableMap, selfKey, stageCtrl );
            foreach( int landingTileIdx in landingTileIndices )
            {
                int targetTileIdx = landingTileIdx + dirOffset;
                if( allEnemyIndices.Contains( targetTileIdx ) && IsAttackableFromLandingTile( landingTileIdx, targetTileIdx, stageCtrl ) )
                {
                    Methods.SetBitFlag( ref attackableMap[targetTileIdx].Flag, TileBitFlag.ATTACKABLE_TARGET_EXIST );
                }
            }
        }

        /// <summary>
        /// 着地先(ゴースト)のタイルを決定します。着地先として選べる距離内で、ジャンプで着地可能な最も遠いタイルを返します。
        /// 着地可能なタイルが存在しない場合は-1を返します。
        /// </summary>
        private static int ResolveLandingTile( int dprtIdx, ActionableTileData actionableTileData, StageController stageCtrl, CharacterKey selfKey, int landingRange, int jumpForce )
        {
            var targetableMap = actionableTileData.TargetableTileMap;
            if( targetableMap.Count <= 0 ) { return -1; }

            // ターゲット可能タイルは全て同一方向の直線上にあるため、最も近いタイル(隣接タイル)との差分から進行方向を求める
            int nearestTileIdx = targetableMap.Keys.OrderBy( idx => stageCtrl.CalculateTotalRange( dprtIdx, idx ) ).First();
            if( stageCtrl.CalculateTotalRange( dprtIdx, nearestTileIdx ) != 1 ) { return -1; }

            int dirOffset = nearestTileIdx - dprtIdx;
            int maxL      = Math.Min( landingRange, targetableMap.Keys.Max( idx => stageCtrl.CalculateTotalRange( dprtIdx, idx ) ) );

            var landingTileIndices = ScanLandingTiles( dprtIdx, dirOffset, maxL, jumpForce, targetableMap, selfKey, stageCtrl );

            return ( 0 < landingTileIndices.Count ) ? landingTileIndices[^1] : -1;
        }

        /// <summary>
        /// 指定方向へ1マスずつ走査し、ジャンプで着地可能なタイルを近い順に求めます。
        /// 攻撃対象の絞り込みと着地先の決定で同じ条件を用いるための共通処理です。
        /// 判定条件:
        ///   飛び越え不可: n + JumpForce - x < h (キャラクターが存在するタイルは h にキャラクターの高さを加える)。以降のタイルへは到達不可
        ///   着地タイル:   飛び越え不可でなく、自身が立てる(TileDynamicData.IsStandableBy)タイル(味方・敵を問わずキャラクターは飛び越えられる)
        ///                 かつ、自身のタイルとの高低差が通常移動と同じ基準(TileDataHandler.IsWithinJumpHeight)で移動可能なタイル。
        ///                 低すぎて着地できないタイルは飛び越えることは出来るため、走査は継続する
        /// </summary>
        /// <param name="tileMap">走査するタイルの範囲。含まれないタイルに達した時点で走査を終了する</param>
        /// <param name="selfKey">スキル使用者のキー(着地予約等、立てるタイルかどうかの判定に用いる)</param>
        private static List<int> ScanLandingTiles( int dprtIdx, int dirOffset, int maxL, int jumpForce, Dictionary<int, TileDynamicData> tileMap, CharacterKey selfKey, StageController stageCtrl )
        {
            var landingTileIndices = new List<int>();
            float selfHeight       = stageCtrl.GetTileStaticData( dprtIdx ).Height;

            for( int l = 1; l <= maxL; ++l )
            {
                int tileIdx = dprtIdx + l * dirOffset;
                if( !tileMap.TryGetValue( tileIdx, out var tileData ) ) { break; }

                // 飛び越えられないタイルに達した場合、以降のタイルへも到達不可
                if( IsBlockingJump( selfHeight, jumpForce, l, tileIdx, stageCtrl ) ) { break; }

                float landingHeight = stageCtrl.GetTileStaticData( tileIdx ).Height;
                if( tileData.IsStandableBy( selfKey ) && TileDataHandler.IsWithinJumpHeight( selfHeight, landingHeight, jumpForce ) )
                {
                    landingTileIndices.Add( tileIdx );
                }
            }

            return landingTileIndices;
        }

        /// <summary>
        /// 着地タイルから、その直後のタイルにいる相手を攻撃出来るかを判定します(高さの差が一定以内である必要があります)
        /// </summary>
        private static bool IsAttackableFromLandingTile( int landingTileIdx, int targetTileIdx, StageController stageCtrl )
        {
            float landingHeight = stageCtrl.GetTileStaticData( landingTileIdx ).Height;
            float targetHeight  = stageCtrl.GetTileStaticData( targetTileIdx ).Height;
            return Mathf.Abs( targetHeight - landingHeight ) <= Constants.JUMP_SLASH_ATTACKABLE_HEIGHT_DIFF;
        }

        private enum JumpSlashState
        {
            START,
            JUMPING,
            WAIT_END,
            END
        }

        private JumpSlashState _state;
        private bool _isAtLatter;
        private bool _isPassedApex;
        private Vector3 _xzVelocity;
        // 着地の何秒前に斬りかかりモーションを開始するか。モーションの長さが取得出来なかった場合は負の値(頂点通過時に開始する)
        private float _latterLeadTime;

        [Inject]
        public JumpSlashSA( Character owner, List<CharacterKey> targetCharaKeys, BattleRoutineController btlRtnCtrl, StageController stageCtrl )
            : base( owner, targetCharaKeys, btlRtnCtrl, stageCtrl )
        {
            _attackAnimTag = AnimDatas.AnimeConditionsTag.DASH_AND_JUMP_ATK_LATTER;
            _skillID       = SkillID.JUMP_SLASH;
        }

        protected override void StartAction()
        {
            base.StartAction();

            _isAtLatter     = false;
            _isPassedApex   = false;
            _latterLeadTime = ResolveLatterLeadTime();

            _state = JumpSlashState.START;
        }

        protected override void UpdateAction()
        {
            base.UpdateAction();

            switch( _state )
            {
                case JumpSlashState.START:
                    {
                        _owner.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.DASH_AND_JUMP_ATK_INITIAL );

                        // 着地タイルへの放物運動を開始する
                        // XZ 方向の速度を設定した後、StartSkillJump で Y 方向の初速・加速度を上書きする
                        var startPos = _owner.GetPosition();
                        var xzDir = ( _goalPosition - startPos ).XZ().normalized;
                        _xzVelocity = xzDir * Constants.JUMP_SLASH_INITIAL_SPEED;
                        _owner.SetVelocityAndAcceleration( _xzVelocity, Vector3.zero );
                        float speedRate = Constants.JUMP_SLASH_INITIAL_SPEED / Constants.CHARACTER_MOVE_SPEED;
                        _owner.StartSkillJump( startPos, _goalPosition, speedRate );
                        // フレームが飛んだ際に軌道の途中まで一気に進まないよう、着地までの経過時間に上限を設ける
                        _owner.LimitMoveDeltaTime( Constants.JUMP_SLASH_MAX_DELTA_TIME );

                        _state = JumpSlashState.JUMPING;
                        break;
                    }
                case JumpSlashState.JUMPING:
                    {
                        // 前フレームより Y 座標が下がった時点を放物運動の頂点通過とみなす
                        if( !_isPassedApex && _owner.GetPosition().y < _owner.GetPreviousPosition().y )
                        {
                            _isPassedApex = true;

                            // 頂点を過ぎて降下(＝被弾)に転じるタイミングでカメラを寄りアングルへ遷移させる
                            _cameraProcess?.TransitNextPhase();
                        }

                        // 着地までの残り時間が斬りかかりモーションの先行時間を下回った時点で、モーションを開始する
                        // (モーションの長さが取得出来なかった場合は、頂点通過時に開始する)
                        if( !_isAtLatter && ( ( 0f <= _latterLeadTime ) ? CalcurateRemainingTimeToLand() <= _latterLeadTime : _isPassedApex ) )
                        {
                            StartLatterAnimation();
                        }

                        // UpdateAttack2TargetCharacters();
                        UpdateAttackAnimEnd();

                        if( Methods.IsPassedPosition( _owner.GetPosition(), _goalPosition, _xzVelocity ) )
                        {
                            // 万一モーションを開始しないまま着地した場合でも、モーションの終了待ちで停止しないよう開始しておく
                            if( !_isAtLatter ) { StartLatterAnimation(); }

                            UpdateAttack2TargetCharacters();

                            _owner.ResetVelocityAcceleration();
                            _owner.ResetMoveDeltaTimeLimit();
                            _owner.SetPosition( _goalPosition );
                            _owner.BattleParams.TmpParam.CurrentTileIndex = _goalTileIndex;
                            _state = JumpSlashState.WAIT_END;
                        }
                        break;
                    }
                case JumpSlashState.WAIT_END:
                    {
                        UpdateAttackAnimEnd();
                        if( _isAttackAnimEnded )
                        {
                            _owner.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.WAIT );
                            _state = JumpSlashState.END;
                        }
                        break;
                    }
                case JumpSlashState.END:
                    EndAction();
                    break;
            }
        }

        private void StartLatterAnimation()
        {
            _owner.AnimCtrl.SetAnimator( AnimDatas.AnimeConditionsTag.DASH_AND_JUMP_ATK_LATTER );
            _isAtLatter = true;
        }

        /// <summary>
        /// 斬りかかりモーションを着地の何秒前に開始するかを求めます。
        /// モーションの打撃のタイミング(SkillStrikeOnAnimEvent)で着地するよう、モーションの開始から打撃までの時間を用います。
        /// 打撃のイベントが設定されていない場合はモーションの終了時に着地するようモーションの長さを用い、
        /// それも取得出来ない場合は負の値を返します。
        /// </summary>
        private float ResolveLatterLeadTime()
        {
            const AnimDatas.AnimeConditionsTag latterTag = AnimDatas.AnimeConditionsTag.DASH_AND_JUMP_ATK_LATTER;

            if( _owner.AnimCtrl.TryGetEventTime( latterTag, nameof( BattleAnimationEventReceiver.SkillStrikeOnAnimEvent ), out float strikeTime ) ) { return strikeTime; }

            if( _owner.AnimCtrl.TryGetAnimationLength( latterTag, out float latterLength ) )
            {
                Debug.LogWarning( $"[JumpSlashSA] {_owner.name} の斬りかかりモーションに打撃のイベント(SkillStrikeOnAnimEvent)が設定されていないため、モーションの終了時に着地させます。" );
                return latterLength;
            }

            Debug.LogWarning( $"[JumpSlashSA] {_owner.name} の斬りかかりモーションの長さが取得出来ないため、頂点通過時にモーションを開始します。(AnimStateLengthTable を再収集してください)" );
            return -1f;
        }

        /// <summary>
        /// 着地までの残り時間を求めます(XZ 方向は等速で移動するため、残りの水平距離から求められます)
        /// </summary>
        private float CalcurateRemainingTimeToLand()
        {
            return ( _goalPosition - _owner.GetPosition() ).XZ().magnitude / Constants.JUMP_SLASH_INITIAL_SPEED;
        }

        protected override bool IsFinished()
        {
            return _state == JumpSlashState.END;
        }

        // スキル使用者と攻撃対象の Y 座標の差が 1.0f 未満になった際を攻撃判定タイミングとする
        protected override bool IsInAttackRange( Character target )
        {
            float ownerY  = _owner.GetPosition().y;
            float targetY = target.GetPosition().y;
            return Mathf.Abs( ownerY - targetY ) < 1.0f;
        }
    }
}
