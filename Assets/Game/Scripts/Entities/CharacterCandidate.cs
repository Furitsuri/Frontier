using UnityEngine;
using static Constants;

namespace Frontier.Entities
{
    /// <summary>
    /// 配置候補キャラクターを表すクラス
    /// </summary>
    public class CharacterCandidate
    {
        private Character _character;
        private Texture2D _snapShotImg;
        public bool IsSelected { get; set; }
        public Character Character => _character;
        public Texture2D SnapshotImg => _snapShotImg;

        public void Init( Character character, Texture2D snapShotImg )
        {
            _character      = character;
            _snapShotImg    = snapShotImg;
            IsSelected      = false;

            _character.gameObject.SetActive( null == snapShotImg );
            var reservePos = new Vector3( CHARACTER_SELECTION_SPACING_X * _character.GetStatusRef.characterIndex, CHARACTER_SELECTION_OFFSET_Y, CHARACTER_SELECTION_OFFSET_Z );
            _character.GetTransformHandler.SetPosition( reservePos );
        }
    }
}