using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Entities
{
    /// <summary>
    /// キャラクターの画像(スナップショット)の撮影を一手に担う共有シングルトンです。
    /// 撮影用のカメラ(EntitySnapshot)はこのクラスだけが保持するため、キャラクターを撮影する処理は必ずこのクラスを経由させてください。
    /// ・顔アイコン用の画像(GetOrCapture / CaptureAll): 画面から見切れている敵を示すアイコン等に使用します。キャラクター毎に保持し、
    ///   戦闘に最初から登場する敵・第三勢力の分は、戦闘の初期化時(BattleRoutineController.Init。ローディング画面が解除される前)に
    ///   まとめて撮影しておきます。増援等、その時点で存在していなかったキャラクターの分は、初回の取得時にその場で撮影します。
    /// ・全身の画像(CaptureFullBody): 配置フェーズのキャラクター選択画面に使用します。大きさが呼び出し側のUIに依存するため保持はせず、
    ///   呼び出しの度に撮影して返します。
    /// MEMO : 撮影の際はキャラクターを一時的に撮影用の位置へ移して元へ戻すため(EntitySnapshot)、
    ///        キャラクターが歩いている途中など、位置が変化している最中の撮影は避けてください。
    /// </summary>
    public class CharacterPortraitRegistry
    {
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private EntitySnapshot _entitySnapshot = null;
        private readonly Dictionary<Character, Texture2D> _portraits = new Dictionary<Character, Texture2D>();

        [Inject]
        public CharacterPortraitRegistry() { }

        /// <summary>
        /// 指定の各キャラクターの顔アイコン用の画像を、まとめて撮影します(撮影済みのキャラクターは対象外)。
        /// 戦闘中に撮影の負荷が掛からないよう、画面が表示される前に呼び出してください。
        /// </summary>
        public void CaptureAll( IEnumerable<Character> characters )
        {
            foreach( var character in characters )
            {
                GetOrCapture( character );
            }
        }

        /// <summary>
        /// 指定キャラクターの顔アイコン用の画像を取得します。未撮影の場合は、その場で撮影します。
        /// </summary>
        public Texture2D GetOrCapture( Character character )
        {
            if( _portraits.TryGetValue( character, out Texture2D portrait ) && null != portrait ) { return portrait; }

            Snapshot.CapturePortrait( CHARACTER_PORTRAIT_SNAPSHOT_SIZE, character, out portrait );
            _portraits[character] = portrait;

            return portrait;
        }

        /// <summary>
        /// 指定キャラクターの全身の画像を、指定の大きさで撮影します。
        /// 撮影した画像は保持しないため、呼び出し側で管理してください。
        /// </summary>
        /// <param name="textureWidth">撮影する画像の幅(ピクセル)</param>
        /// <param name="textureHeight">撮影する画像の高さ(ピクセル)</param>
        public Texture2D CaptureFullBody( Character character, int textureWidth, int textureHeight )
        {
            Snapshot.CaptureCharacter( textureWidth, textureHeight, character, out Texture2D snapshot, false, AnimDatas.AnimeConditionsTag.WAIT );

            return snapshot;
        }

        /// <summary>
        /// 撮影用のカメラ。初回の撮影時に1つだけ生成し、以降は使い回します
        /// </summary>
        private EntitySnapshot Snapshot
        {
            get
            {
                if( null == _entitySnapshot )
                {
                    _entitySnapshot = _hierarchyBld.InstantiateWithDiContainer<EntitySnapshot>( false );
                    _entitySnapshot.Init();
                }

                return _entitySnapshot;
            }
        }
    }
}
