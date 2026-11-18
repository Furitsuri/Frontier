using Frontier.Combat;
using Frontier.Combat.Skill;
using Frontier.Loaders;
using Frontier.Registries;
using Frontier.Stage;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Entities
{
    [SerializeField]
    public class Character : Entity
    {
        [Header( "弾オブジェクト" )]
        [SerializeField] private GameObject _bulletObject;

        [Header( "ステータス" )]
        [SerializeField] protected Status _status;

        /// <summary>
        /// 本来ならBattleLogicBaseに持たせるべきですが、BattleLogicBaseは戦闘ルーチン開始後にキャラクターに追加されるものであり、
        /// Unityの仕様上、実行後に追加したコンポーネントについては[SerializeField]や[Serializable]が設定されていても、
        /// Inspector上に表示されないため、仕方なくパラメータ群をここで定義しています。
        /// </summary>
        [Header( "戦闘用一時パラメータ" )]
        [SerializeField] private BattleParameters _btlParams;

        [Header( "戦闘表示用カメラパラメータ" )]
        [SerializeField] private CameraParameter _camParam;

        [Inject] protected ICharacterUiFeedback _uiFeedback                 = null;
        [Inject] protected PrefabRegistry _prefabReg                        = null;
        [Inject] protected TimeScaleController _timeScaleCtrl               = null;

        private CharacterParameterPresenter _parameterPresenter = null;
        private readonly TimeScale _timeScale                   = new TimeScale();
        private List<(Material material, Color originalColor)> _textureMaterialsAndColors   = new List<(Material, Color)>();
        protected FieldLogicBase _fieldLogic                                                = null;
        protected BattleLogicBase _battleLogic                                              = null;
        protected BattleAnimationEventReceiver _animReceiver                                = null;
        // 移動を伴うスキルの着地先(対象選択中のプレビュー・予約した着地先)を示すゴースト。
        // 初回の使用時に生成し、以降はキャラクターが破棄されるまで保持して、表示・非表示と位置のみを切り替えて使い回す
        private GhostObject _ghostObject                                                    = null;
        protected AnimationController _animCtrl                                             = null;     // アニメーションコントローラ
        protected Bullet _bullet                                                            = null;     // 矢などの弾

        public int StatusEffectBitFlag { get; set; } = 0;                           // キャラクターに設定されているステータス効果のビットフラグ
        public float ElapsedTime { get; set; } = 0f;
        public AnimationController AnimCtrl => _animCtrl;                           // アニメーションコントローラの取得
        public TimeScale GetTimeScale => _timeScale;                                // タイムスケールの取得
        public BattleParameters BattleParams => _btlParams;                         // パラメータ群の取得(※CharacterParametersはstructなので参照渡しにする)
        public BattleLogicBase BattleLogic => _battleLogic;
        public BattleAnimationEventReceiver BtlAnimReceiver => _animReceiver;
        public ref Status GetStatusRef => ref _status;
        public ref CameraParameter CameraParam => ref _camParam;

        void LateUpdate()
        {
        }

        /// <summary>
        /// タイル間移動専用のジャンプ Y 成分を設定します。
        /// 高低差がある場合のみ意図通りの弧を描きます。XZ 速度は事前に SetVelocityAndAcceleration で設定しておく必要があります。
        /// </summary>
        public void StartTileMoveJump( in Vector3 departingPosition, in Vector3 destinationPosition, float moveSpeedRate )
        {
            var diffPosition   = destinationPosition - departingPosition;
            var diffPositionXZ = diffPosition.XZ();
            float diffHeight   = diffPosition.y;
            float arrivalTime  = diffPositionXZ.magnitude / ( moveSpeedRate * CHARACTER_MOVE_SPEED );

            float vy, ay;
            if( 0 < diffHeight )
            {
                // MEMO : 放物運動によって、一度最高点まで上昇し、そこから目的地まで落下する動作を実装する
                // 到達時刻TはXZ平面上の動きが等速であることから既に定められているため、ここでは初速度と落下加速度を求める

                // 1. 落下加速度gを求める( 基本公式 : h = v0 * t + 0.5 * g * t^2 → 0.5 * g * t^2 + v0 * t - h = 0 )
                //    解の和と積より、 t1 + t2 = - 2 * v0 / g, t1 * t2 = - 2 * h / g → t1 = - 2 * h / ( g * t2 ) より v0 = h / t2 - 0.5 * g * t2
                //    t1 < t2 であることが前提のため、上記 t1 = - 2 * h / ( g * t2 ) に t1 < t2 を代入したとき、
                //    - 2 * h / ( g * t2 ) < t2 → g < - 2 * h / t2^2 となる(gは負の値であることが分かっているため、等号は反対となる)
                //    よって、g = - 2 * h / t2^2 とし、そこに任意の負の値を加えることで、t1 < t2 を満たす g を求めることができる
                ay  = -2 * diffHeight / Mathf.Pow( arrivalTime, 2f );
                ay -= JUMP_POSITIVE_Y_ACCELERATION; // t1 < t2 を満たすために、任意の負の値を加える

                // 2. 初速度v0を求める( 1における導出より v0 = h / t2 - 0.5 * g * t2 )
                vy  = diffHeight / arrivalTime - 0.5f * ay * arrivalTime;
            }
            else
            {
                // MEMO : 目的地が出発地点よりも低い場合は、単純に等加速度運動で落下する動作を実装する

                // h = v0 * t + 0.5 * g * t^2 が成り立つ
                // 0 < v0 が前提であり、それを満たす g を求める。v0には任意の正の値を代入する
                vy = JUMP_NEGATIVE_Y_VELOCITY;
                // 0.5 * g * t^2 + v0 * t - h = 0 より、 g = 2 * ( - v0 * t + h ) / t^2
                ay = 2 * ( -vy * arrivalTime + diffHeight ) / ( arrivalTime * arrivalTime );
            }

            _transformHdlr.SetVerticalMotion( vy, ay );
        }

        /// <summary>
        /// スキル攻撃専用の放物運動 Y 成分を設定します。
        /// 高低差がゼロの場合でも常に JUMP_SKILL_PEAK_HEIGHT 分の弧を描きます。
        /// XZ 速度は事前に SetVelocityAndAcceleration で設定しておく必要があります。
        /// </summary>
        public void StartSkillJump( in Vector3 departingPosition, in Vector3 destinationPosition, float moveSpeedRate )
        {
            var diffPositionXZ = ( destinationPosition - departingPosition ).XZ();
            float diffHeight   = destinationPosition.y - departingPosition.y;
            float arrivalTime  = diffPositionXZ.magnitude / ( moveSpeedRate * CHARACTER_MOVE_SPEED );

            // MEMO : 高低差に関わらず必ず弧を描くよう、出発点・着地点のうち高い方を基準に
            //        JUMP_SKILL_PEAK_HEIGHT 分だけ上乗せした相対高さを目標頂点とする
            // 対称弧（頂点を t = T/2 と仮定）から v0 を求め、到達高さ条件から g を求める
            //   対称弧の場合 : pos(T/2) = peakRelHeight → v0*(T/2) + 0.5*g*(T/2)^2 = peakRelHeight
            //   t=T/2 が頂点ならば v0 + g*(T/2) = 0 → v0 = -g*(T/2)
            //   代入すると : -g*(T/2)^2 + 0.5*g*(T/2)^2 = peakRelHeight → -0.5*g*(T/2)^2 = peakRelHeight
            //   → v0 = 4 * peakRelHeight / T
            float peakRelHeight = Mathf.Max( departingPosition.y, destinationPosition.y ) - departingPosition.y + JUMP_SKILL_PEAK_HEIGHT;
            float vy = 4f * peakRelHeight / arrivalTime;

            // 到達時の高さ条件: diffHeight = v0*T + 0.5*g*T^2 → g = 2*(diffHeight - v0*T) / T^2
            float ay = 2f * ( diffHeight - vy * arrivalTime ) / ( arrivalTime * arrivalTime );

            _transformHdlr.SetVerticalMotion( vy, ay );
        }

        /// <summary>
        /// キャラクターに対する時間計測をリセットします
        /// </summary>
        public void ResetElapsedTime()
        {
            ElapsedTime = 0;
        }

        public void Apply( BattleFileLoader.CharacterDeployData statusData )
        {
            Status.ApplyParams( ref _status, in statusData );

            // 装備中のパッシブスキルによる補正を、戦闘開始前(配置画面等)のパラメータ表示にも反映しておく
            RefreshModifiedParam();

            // スキルの使用可否フラグを更新
            RefreshUseableSkillFlags( SituationType.NONE, 0xff );
        }

        /// <summary>
        /// 現在のステータス(装備中のパッシブスキル等)から補正値(BattleParams.ModifiedParam)を算出し直します。
        /// ステータスの設定・装備の変更後など、戦闘開始前でも補正値をパラメータ表示に反映したい場合に呼び出してください。
        /// </summary>
        public void RefreshModifiedParam()
        {
            _btlParams.RefreshModifiedParam( _status );
        }

        public void RegistParameterPresenter( CharacterParameterPresenter presenter )
        {
            _parameterPresenter = presenter;
        }

        /// <param name="checkAttackTarget">
        /// trueの場合、対象選択に遷移するタイプのスキル(攻撃系)について、効果範囲内に実際に攻撃対象が
        /// 存在するかどうかも使用可否判定に含めます。配置画面やステータス表示など、位置に関わらず
        /// 装備状況そのものを示したい場面ではfalse(既定)のままにしてください。PlSelectSkillStateの
        /// スキル選択画面のように、その場でのプレビュー・選択可否を示したい場合にのみtrueを指定します。
        /// </param>
        public void RefreshUseableSkillFlags( SituationType situationType, int useableActionTypeBit = 0xff, bool checkAttackTarget = false )
        {
            int equipSkillIndexTransitAction = -1;
            _battleLogic?.IsSkillToggledTransitActionState( out equipSkillIndexTransitAction );

            for( int i = 0; i < EQUIPABLE_ACTIVE_SKILL_MAX_NUM; ++i )
            {
                ActiveSkillID skillID = GetEquipSkillID( i );
                if( !ActiveSkillsData.IsValidSkill( skillID ) ) { continue; }
                var skillData = ActiveSkillsData.data[( int ) skillID];

                // スキル使用ONの状態であればOFFにするだけなので、チェックする必要がない
                if( BattleParams.TmpParam.IsSkillsToggledON[i] )
                {
                    BattleParams.TmpParam.IsUseableSkill[i] = true;
                    continue;
                }

                BattleParams.TmpParam.IsUseableSkill[i] = false;
                
                if( BattleParams.TmpParam.IsSkillsUsed[i] ||                                                                    // 既に使用済みのスキルは使用不可
                    ( SituationType.NONE != situationType && skillData.SituationType != situationType ) ||                      // 同一のシチュエーションでない場合は使用不可(攻撃シチュエーション時に防御スキルは使用出来ない等)
                    !Methods.HasAnyFlag( useableActionTypeBit, skillData.ActionType ) ||                                      // スキルの種類が、使用可能なスキルの種類のビットフラグに含まれていない場合は使用不可
                    ( 0 <= equipSkillIndexTransitAction && ActiveSkillsData.IsTransitionSkillActionType( skillData.ActionType ) ) ||  // 対象選択に遷移するスキルの使用フラグがONの状態では、同様のスキルは使用不可
                    _status.CurActionGauge < BattleParams.TmpParam.ActGaugeConsumption + skillData.Cost )                       // コストが現在のアクションゲージ値を越えていないかをチェック
                {
                    continue;
                }

                // checkAttackTarget指定時のみ、ターゲット選択に遷移するタイプのスキル(攻撃系)について
                // 実際に効果範囲内に攻撃対象が存在する場合のみ使用可能とする
                // (移動を伴うスキルは攻撃対象が存在しなくても、着地可能なタイルがあれば使用可能とする)
                // ( BattleLogicがまだ設定されていない場合(戦闘開始前のステータス反映時など)は、位置情報が不定のため判定をスキップする )
                if( checkAttackTarget &&
                    ActiveSkillsData.IsTransitionSkillActionType( skillData.ActionType ) &&
                    null != _battleLogic &&
                    !_battleLogic.ActionRangeCtrl.IsSkillExecutableAt( BattleParams.TmpParam.CurrentTileIndex, skillID ) )
                {
                    continue;
                }

                BattleParams.TmpParam.IsUseableSkill[i] = true;
            }

            _parameterPresenter?.RefreshParamRender( this, _status, _btlParams.ModifiedParam );
        }

        public void RestoreMaterialsOriginalColor()
        {
            foreach( var (material, originalColor) in _textureMaterialsAndColors )
            {
                material.color = originalColor;
                material.SetFloat( "_Surface", 0f );
                material.DisableKeyword( "_SURFACE_TYPE_TRANSPARENT" );
                material.SetOverrideTag( "RenderType", "Opaque" );
                material.renderQueue = ( int ) UnityEngine.Rendering.RenderQueue.Geometry;
            }
        }

        /// <summary>
        /// ゴーストの表示・非表示を切り替えます(ゴーストを生成していない場合は何もしません)。
        /// 非表示にしても破棄はしないため、着地先のタイル(GhostObject.TileIndex)・位置は保持されます
        /// </summary>
        public void SetGhostActive( bool isActive )
        {
            if( _ghostObject != null )
            {
                _ghostObject.gameObject.SetActive( isActive );
            }
        }

        /// <summary>
        /// 表示中のゴーストのタイルを取得します。
        /// ゴーストは非表示にしても以前のタイルを保持したまま残るため、着地先の判定には必ずこちらを用いてください
        /// </summary>
        /// <returns>表示中のゴーストのタイル。ゴーストを表示していない場合は-1</returns>
        public int GetActiveGhostTileIndex()
        {
            return ( _ghostObject != null && _ghostObject.gameObject.activeSelf ) ? _ghostObject.TileIndex : -1;
        }

        /// <summary>
        /// 指定のタイルにゴーストを配置して表示します
        /// </summary>
        /// <param name="tileIndex">配置するタイル</param>
        /// <param name="position">配置する位置</param>
        /// <param name="rotation">配置する向き</param>
        public void ShowGhostAt( int tileIndex, Vector3 position, Quaternion rotation )
        {
            var ghost       = GetGhostObject();
            ghost.TileIndex = tileIndex;
            ghost.transform.SetPositionAndRotation( position, rotation );
            ghost.gameObject.SetActive( true );
        }

        public void SetMaterialsSemiTransparent( float alpha = 0.5f )
        {
            foreach( var (material, originalColor) in _textureMaterialsAndColors )
            {
                material.SetFloat( "_Surface", 1f );
                material.EnableKeyword( "_SURFACE_TYPE_TRANSPARENT" );
                material.SetOverrideTag( "RenderType", "Transparent" );
                material.renderQueue = ( int ) UnityEngine.Rendering.RenderQueue.Transparent;
                Color color = originalColor;
                color.a = Mathf.Clamp01( alpha );
                material.color = color;
            }
        }

        public void SetMaterialsGrayColor()
        {
            foreach( var (material, originalColor) in _textureMaterialsAndColors )
            {
                material.color = Color.gray;
            }
        }

        /// <summary>
        /// 指定の装備インデックスに対応するスキルが、使用コストの面で使用可能かどうかを判定します。
        /// MEMO : BattleLogicが生成されていない状態である雇用フェーズでも判定する機会があるため、Character内に定義しています。
        /// </summary>
        /// <param name="skillIdx"></param>
        /// <param name="situationType"></param>
        /// <returns></returns>
        public bool IsUseableSkillByCost( int skillIdx )
        {
            if( skillIdx < 0 || EQUIPABLE_ACTIVE_SKILL_MAX_NUM <= skillIdx )
            {
                Debug.Assert( false, "指定されているスキルの装備インデックス値がスキルの装備範囲を超えています。" );

                return false;
            }

            ActiveSkillID skillID = GetEquipSkillID( skillIdx );
            if( !ActiveSkillsData.IsValidSkill( skillID ) )                                   { return false; }
            var skillData = ActiveSkillsData.data[( int ) skillID];
            // コストが現在のアクションゲージ値を越えていないかをチェック
            if( _status.CurActionGauge < BattleParams.TmpParam.ActGaugeConsumption + skillData.Cost ) { return false; }

            return true;
        }

        public ActiveSkillID GetEquipSkillID( int slotIdx )
        {
            if( slotIdx < 0 || EQUIPABLE_ACTIVE_SKILL_MAX_NUM <= slotIdx )
            {
                Debug.Assert( false, "指定されているスキルの装備インデックス値がスキルの装備範囲を超えています。" );
                return ActiveSkillID.NONE;
            }
            return _status.EquipSkills[slotIdx];
        }

        /// <summary>
        /// 指定のパッシブスキル枠に装備されているスキルを取得します
        /// </summary>
        /// <param name="slotIdx">パッシブスキル枠のインデックス(0〜EQUIPABLE_PASSIVE_SKILL_MAX_NUM-1)</param>
        /// <returns>装備中のスキルID(未装備の場合はPassiveSkillID.NONE)</returns>
        public PassiveSkillID GetPassiveSkillID( int slotIdx )
        {
            if( slotIdx < 0 || EQUIPABLE_PASSIVE_SKILL_MAX_NUM <= slotIdx )
            {
                Debug.Assert( false, "指定されているスキルの装備インデックス値がパッシブスキルの装備範囲を超えています。" );
                return PassiveSkillID.NONE;
            }
            // MEMO : パッシブスキル枠追加以前のデータから生成されたステータスでは枠が未確保の場合があるため、未装備扱いとします
            if( _status.PassiveSkills == null || _status.PassiveSkills.Length <= slotIdx ) { return PassiveSkillID.NONE; }

            return _status.PassiveSkills[slotIdx];
        }

        /// <summary>
        /// 指定のパッシブスキルを装備しているかを取得します
        /// 戦闘中の常時効果の適用や、フィールドイベント等でのパッシブスキルの有無の判定に用います
        /// </summary>
        /// <param name="skillID">判定するスキルID</param>
        /// <returns>パッシブスキル枠のいずれかに装備しているか否か</returns>
        public bool HasPassiveSkill( PassiveSkillID skillID )
        {
            if( !PassiveSkillsData.IsValidSkill( skillID ) ) { return false; }

            for( int i = 0; i < EQUIPABLE_PASSIVE_SKILL_MAX_NUM; ++i )
            {
                if( GetPassiveSkillID( i ) == skillID ) { return true; }
            }

            return false;
        }

        public CHARACTER_TAG GetCharacterTag()
        {
            return _status.characterTag;
        }

        public CharacterKey GetCharacterKey()
        {
            return new CharacterKey( _status.characterTag, _status.characterIndex );
        }

        /// <summary>
        /// 設定されている弾を取得します
        /// </summary>
        /// <returns>Prefabに設定されている弾</returns>
        public Bullet GetBullet() { return _bullet; }

        /// <summary>
        /// ゴーストを取得します。未生成の場合は非表示の状態で生成します(表示はSetGhostActive・ShowGhostAtで行います)
        /// </summary>
        public GhostObject GetGhostObject()
        {
            if( null == _ghostObject )
            {
                // ゴーストは生成時点の姿勢を写し取り、以降も使い回すため、待機の姿勢を取らせた上で生成する
                // (移動・攻撃・合図のモーションには跳ねるものがあり、その途中の姿勢を写し取ると着地先から浮いて見えるため)
                System.Action create = () => _ghostObject = GhostObject.Create( this.transform, gameObject.name );
                if( _animCtrl != null ) { _animCtrl.InvokeWithPoseOf( AnimDatas.AnimeConditionsTag.WAIT, create ); }
                else { create(); }
                _ghostObject.gameObject.SetActive( false );
            }

            return _ghostObject;
        }

        virtual public void Setup()
        {
            LazyInject.GetOrCreate( ref _btlParams, () => _hierarchyBld.InstantiateWithDiContainer<BattleParameters>( false ) );
            LazyInject.GetOrCreate( ref _animCtrl, () => _hierarchyBld.InstantiateWithDiContainer<AnimationController>( false ) );

            if( _bulletObject != null )
            {
                LazyInject.GetOrCreate( ref _bullet, () => _hierarchyBld.CreateComponentNestedNewDirectoryWithDiContainer<Bullet>( _bulletObject, this.gameObject, "Bullet", false, false ) );
            }

            _status.Setup();
            _btlParams.Setup();

            _animCtrl.Regist( GetComponent<Animator>() );   // アニメーションコントローラにプレハブに登録されたアニメーションを登録
            _timeScaleCtrl.Regist( _timeScale );            // 戦闘時間管理クラスに自身の時間管理クラスを登録
        }

        /// <summary>
        /// 初期化処理を行います
        /// </summary>
        public override void Init()
        {
            base.Init();
            _status.Init();
            _btlParams.Init();
            _camParam.Init();

            _timeScale.OnValueChange = AnimCtrl.UpdateTimeScale;

            ResetElapsedTime();
            // キャラクターモデルのマテリアルが設定されているObjectを取得し、Materialと初期のColor設定を保存
            RegistMaterialsRecursively( this.transform, OBJECT_TAG_NAME_CHARA_SKIN_MESH );
        }

        /// <summary>
        /// 破棄処理を行います
        /// </summary>
        public override void Dispose()
        {
            if( _isDisposed ) { return; }

            // 戦闘時間管理クラスの登録を解除（DI未注入のゴーストオブジェクトからも呼ばれる可能性があるため null チェック）
            _timeScaleCtrl?.Unregist( _timeScale );

            if ( _bullet != null )
            {
                _bullet.Dispose();
                _bullet = null;
            }

            _fieldLogic?.Dispose();
            _fieldLogic = null;
            _battleLogic?.Dispose();
            _battleLogic = null;

            DisposeGhost();

            base.Dispose();
        }

        virtual public void OnFieldEnter() { }

        virtual public void OnFieldExit() { }

        virtual public void OnBattleEnter( BattleCameraController btlCamCtrl )
        {
            LazyInject.GetOrCreate( ref _animReceiver, () => _hierarchyBld.AddComponentWithDi<BattleAnimationEventReceiver>( gameObject ) );
            _animReceiver.Regist( this, btlCamCtrl );

            _uiFeedback.ShowHpGaugeOnCharacter( this );
        }

        virtual public void OnBattleExit()
        {
            _uiFeedback.RemoveHpGaugeOnCharacter( this );
            DisposeGhost(); // 戦闘中のみ用いるため、戦闘を終える際に破棄する

            if( _animReceiver != null )
            {
                _animReceiver.Dispose();
                Destroy( _animReceiver );
                _animReceiver = null;
            }
        }

        /// <summary>
        /// ゴーストを破棄します。ゴーストは使い回すため、キャラクターの破棄・戦闘の終了時にのみ呼び出します
        /// (表示を消す場合はSetGhostActive( false )を用いてください)
        /// </summary>
        private void DisposeGhost()
        {
            if( _ghostObject != null )
            {
                _ghostObject.Cleanup();
                _ghostObject = null;
            }
        }

        /// <summary>
        /// 再帰を用いて、指定のタグで登録されているオブジェクトのマテリアルを登録します
        /// ※ 色変更の際に用いる
        /// </summary>
        /// <param name="parent">オブジェクトの親</param>
        /// <param name="tagName">検索するタグ名</param>
        private void RegistMaterialsRecursively( Transform parent, string tagName )
        {
            Transform children = parent.GetComponentInChildren<Transform>();
            foreach( Transform child in children )
            {
                if( child.CompareTag( tagName ) )
                {
                    // モデルによって、マテリアルがMeshとSkinMeshの両方のパターンに登録されているケースがあるため、
                    // どちらも検索する
                    var skinMeshRenderer = child.GetComponent<SkinnedMeshRenderer>();
                    if( skinMeshRenderer != null )
                    {
                        _textureMaterialsAndColors.Add( (skinMeshRenderer.material, skinMeshRenderer.material.color) );
                    }
                    var meshRenderer = child.GetComponent<MeshRenderer>();
                    if( meshRenderer != null )
                    {
                        _textureMaterialsAndColors.Add( (meshRenderer.material, meshRenderer.material.color) );
                    }
                }

                RegistMaterialsRecursively( child, tagName );
            }
        }
    }
}