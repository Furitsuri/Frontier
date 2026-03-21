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
    public class Character : MonoBehaviour
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

        [Inject] protected IUiSystem _uiSystem                              = null;
        [Inject] protected HierarchyBuilderBase _hierarchyBld               = null;
        [Inject] protected PrefabRegistry _prefabReg                        = null;
        [Inject] protected TimeScaleController _timeScaleCtrl               = null;

        private readonly TimeScale _timeScale   = new TimeScale();
        private List<(Material material, Color originalColor)> _textureMaterialsAndColors   = new List<(Material, Color)>();
        protected FieldLogicBase _fieldLogic                                                = null;
        protected BattleLogicBase _battleLogic                                              = null;
        protected BattleAnimationEventReceiver _animReceiver                                = null;
        protected TransformHandler _transformHdlr                                           = null;     // キャラクターのTransform操作を行うクラス
        protected AnimationController _animCtrl                                             = null;     // アニメーションコントローラ
        protected Bullet _bullet                                                            = null;     // 矢などの弾

        public int StatusEffectBitFlag { get; set; } = 0;                           // キャラクターに設定されているステータス効果のビットフラグ
        public float ElapsedTime { get; set; } = 0f;
        public AnimationController AnimCtrl => _animCtrl;                           // アニメーションコントローラの取得
        public TimeScale GetTimeScale => _timeScale;                                // タイムスケールの取得
        public BattleParameters BattleParams => _btlParams;                         // パラメータ群の取得(※CharacterParametersはstructなので参照渡しにする)
        public TransformHandler GetTransformHandler => _transformHdlr;              // Transform操作クラスの取得
        public BattleLogicBase BattleLogic => _battleLogic;
        public BattleAnimationEventReceiver BtlAnimReceiver => _animReceiver;
        public ref Status GetStatusRef => ref _status;
        public ref CameraParameter CameraParam => ref _camParam;

        void Update()
        {
            _transformHdlr.Update( DeltaTimeProvider.DeltaTime ); // TransformHandlerの更新
        }

        void LateUpdate()
        {
        }

        void OnDestroy()
        {
            Dispose();
        }

        /// <summary>
        /// キャラクターに対する時間計測をリセットします
        /// </summary>
        public void ResetElapsedTime()
        {
            ElapsedTime = 0;
        }

        public void Apply( BattleFileLoader.CharacterStatusData statusData )
        {
            Status.ApplyParams( ref _status, in statusData );

            // スキルの使用可否フラグを更新
            RefreshUseableSkillFlags( SituationType.NONE, 0xff );
        }

        public void RefreshUseableSkillFlags( SituationType situationType, int useableActionTypeBit = 0xff )
        {
            for( int i = 0; i < EQUIPABLE_SKILL_MAX_NUM; ++i )
            {
                SkillID skillID = GetEquipSkillID( i );
                if( !SkillsData.IsValidSkill( skillID ) ) { return; }
                var skillData = SkillsData.data[( int ) skillID];

                // スキル使用ONの状態であればOFFにするだけなので、チェックする必要がない
                if( BattleParams.TmpParam.IsSkillsToggledON[i] )
                {
                    BattleParams.TmpParam.IsUseableSkill[i] = true;
                    continue;
                }

                BattleParams.TmpParam.IsUseableSkill[i] = false;
                
                if( BattleParams.TmpParam.IsSkillsUsed[i] ||                                                // 使用済みのスキルは切替不可
                    ( SituationType.NONE != situationType && skillData.SituationType != situationType ) ||  // 同一のシチュエーションでない場合は使用不可(攻撃シチュエーション時に防御スキルは使用出来ない等)
                    !Methods.CheckBitFlag( useableActionTypeBit, skillData.ActionType ) ||                  // スキルの種類が、使用可能なスキルの種類のビットフラグに含まれていない場合は使用不可
                    _status.CurActionGauge < _status.ActGaugeConsumption + skillData.Cost )                 // コストが現在のアクションゲージ値を越えていないかをチェック
                { 
                    continue;
                }

                BattleParams.TmpParam.IsUseableSkill[i] = true;
            }
        }

        public void RestoreMaterialsOriginalColor()
        {
            foreach( var (material, originalColor) in _textureMaterialsAndColors )
            {
                material.color = originalColor;
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
            if( skillIdx < 0 || EQUIPABLE_SKILL_MAX_NUM <= skillIdx )
            {
                Debug.Assert( false, "指定されているスキルの装備インデックス値がスキルの装備範囲を超えています。" );

                return false;
            }

            SkillID skillID = GetEquipSkillID( skillIdx );
            if( !SkillsData.IsValidSkill( skillID ) )                                   { return false; }
            var skillData = SkillsData.data[( int ) skillID];
            // コストが現在のアクションゲージ値を越えていないかをチェック
            if( _status.CurActionGauge < _status.ActGaugeConsumption + skillData.Cost ) { return false; }

            return true;
        }

        public SkillID GetEquipSkillID( int slotIdx )
        {
            if( slotIdx < 0 || EQUIPABLE_SKILL_MAX_NUM <= slotIdx )
            {
                Debug.Assert( false, "指定されているスキルの装備インデックス値がスキルの装備範囲を超えています。" );
                return SkillID.NONE;
            }
            return _status.EquipSkills[slotIdx];
        }

        public CHARACTER_TAG GetCharacterTag()
        {
            return _status.characterTag;
        }

        public CharacterKey CharaKey()
        {
            return new CharacterKey( _status.characterTag, _status.characterIndex );
        }

        /// <summary>
        /// 設定されている弾を取得します
        /// </summary>
        /// <returns>Prefabに設定されている弾</returns>
        public Bullet GetBullet() { return _bullet; }

        virtual public void Setup()
        {
            LazyInject.GetOrCreate( ref _btlParams, () => _hierarchyBld.InstantiateWithDiContainer<BattleParameters>( false ) );
            LazyInject.GetOrCreate( ref _animCtrl, () => _hierarchyBld.InstantiateWithDiContainer<AnimationController>( false ) );
            LazyInject.GetOrCreate( ref _transformHdlr, () => _hierarchyBld.InstantiateWithDiContainer<TransformHandler>( false ) );

            if( _bulletObject != null )
            {
                LazyInject.GetOrCreate( ref _bullet, () => _hierarchyBld.CreateComponentNestedNewDirectoryWithDiContainer<Bullet>( _bulletObject, this.gameObject, "Bullet", false, false ) );
            }
            
            _status.Setup();
            _btlParams.Setup();

            _transformHdlr.Regist( this.transform );
            _animCtrl.Regist( GetComponent<Animator>() );   // アニメーションコントローラにプレハブに登録されたアニメーションを登録
            _timeScaleCtrl.Regist( _timeScale );            // 戦闘時間管理クラスに自身の時間管理クラスを登録
        }

        /// <summary>
        /// 初期化処理を行います
        /// </summary>
        virtual public void Init()
        {
            _status.Init();
            _btlParams.Init();
            _camParam.Init();
            _transformHdlr.Init();

            _timeScale.OnValueChange = AnimCtrl.UpdateTimeScale;

            ResetElapsedTime();
            // キャラクターモデルのマテリアルが設定されているObjectを取得し、Materialと初期のColor設定を保存
            RegistMaterialsRecursively( this.transform, OBJECT_TAG_NAME_CHARA_SKIN_MESH );
        }

        /// <summary>
        /// 破棄処理を行います
        /// </summary>
        virtual public void Dispose()
        {
            // 戦闘時間管理クラスの登録を解除
            _timeScaleCtrl.Unregist( _timeScale );

            if ( _bullet != null )
            {
                _bullet.Dispose();
                _bullet = null;
            }

            _fieldLogic?.Dispose();
            _battleLogic?.Dispose();

            Destroy( gameObject );
            Destroy( this );
        }

        virtual public void OnFieldEnter() { }

        virtual public void OnFieldExit() { }

        virtual public void OnBattleEnter( BattleCameraController btlCamCtrl )
        {
            LazyInject.GetOrCreate( ref _animReceiver, () => _hierarchyBld.AddComponentWithDi<BattleAnimationEventReceiver>( gameObject ) );
            _animReceiver.Regist( this, btlCamCtrl );
        }

        virtual public void OnBattleExit()
        {
            if( _animReceiver != null )
            {
                _animReceiver.Dispose();
                Destroy( _animReceiver );
                _animReceiver = null;
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