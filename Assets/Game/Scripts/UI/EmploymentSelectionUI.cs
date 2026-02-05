using Frontier.Entities;
using static Constants;

namespace Frontier.UI
{
    /// <summary>
    /// キャラクターを雇用する際のキャラクター選択UI
    /// </summary>
    public sealed class EmploymentSelectionUI : CharacterSelectionUI
    {
        /// <summary>
        /// 雇用候補のキャラクターを表示するための表示オブジェクト配列です。
        /// 実機上ではボタン操作によってスライド上の動きが確認できますが、実際には固定数の表示オブジェクトであり、
        /// スライドが完了した際に表示オブジェクトの内容を書き換えることで実現しています。
        /// </summary>
        private EmploymentSelectionDisplay[] _employmentSelectionDisplays = new EmploymentSelectionDisplay[SHOWABLE_SELECTION_CHARACTERS_NUM];

        /// <summary>
        /// 指定のインデックスの候補キャラクターにおける表示を更新します
        /// </summary>
        /// <param name="index"></param>
        /// <param name="candidate"></param>
        public void RefreshCandidate( int index,  ref CharacterCandidate candidate )
        {
            _employmentSelectionDisplays[index].AssignSelectCandidate( ref candidate );
        }

        public override void Setup()
        {
            base.Setup();

            for( int i = 0; i < SHOWABLE_SELECTION_CHARACTERS_NUM; ++i )
            {
                _employmentSelectionDisplays[i] = _characterSelectionDisplays[i] as EmploymentSelectionDisplay;
            }
        }

        public override void AssignSelectCandidates( ref CharacterCandidate[] selectCandidates )
        {
            for( int i = 0; i < SHOWABLE_SELECTION_CHARACTERS_NUM; ++i )
            {
                if( selectCandidates[i] == null )
                {
                    _employmentSelectionDisplays[i].gameObject.SetActive( false );
                    continue;
                }

                _employmentSelectionDisplays[i].gameObject.SetActive( true );
                _employmentSelectionDisplays[i].AssignSelectCandidate( ref selectCandidates[i] );

                // 先頭と末尾以外はコスト表示を有効化
                _employmentSelectionDisplays[i].SetActiveCostObject( !( i == 0 || i == SHOWABLE_SELECTION_CHARACTERS_NUM - 1 ) );
                // 中央のキャラクターのみフォーカス色にする
                _employmentSelectionDisplays[i].SetFocusedColor( i == SHOWABLE_SELECTION_CHARACTERS_NUM / 2 );
            }
        }
    }
}