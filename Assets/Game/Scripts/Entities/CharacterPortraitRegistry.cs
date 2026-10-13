using System.Collections.Generic;
using UnityEngine;
using Zenject;
using static Constants;

namespace Frontier.Entities
{
    /// <summary>
    /// キャラクターの顔アイコン用の画像(スナップショット)を撮影し、キャラクター毎に保持する共有シングルトンです。
    /// 画面から見切れている敵を示すアイコン等、戦闘中にキャラクターを画像として表示したい箇所から使用します。
    /// 戦闘に最初から登場するキャラクターの分は、戦闘開始前(配置フェーズの開始時。ローディング画面が解除される前)に
    /// CaptureAllでまとめて撮影しておき、以降は保持している画像を返します。
    /// 増援等、その時点で存在していなかったキャラクターの分は、初回の取得時にその場で撮影します。
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

            if( null == _entitySnapshot )
            {
                _entitySnapshot = _hierarchyBld.InstantiateWithDiContainer<EntitySnapshot>( false );
                _entitySnapshot.Init();
            }

            _entitySnapshot.CapturePortrait( CHARACTER_PORTRAIT_SNAPSHOT_SIZE, character, out portrait );
            _portraits[character] = portrait;

            return portrait;
        }
    }
}
