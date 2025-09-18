using Frontier.Stage;
using TMPro;
using UnityEngine;
using static Frontier.DebugTools.StageEditor.StageEditorController;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    public class StageEditorPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject _notifyImage;           // 通知用画像オブジェクト
        [SerializeField] private GameObject[] _editParams;          // エディット用のパラメータ群
        [SerializeField] private TextMeshProUGUI _editModeTextMesh;
        [SerializeField] private TextMeshProUGUI[] _firstParamTextMesh;
        [SerializeField] private TextMeshProUGUI[] _secondParamTextMesh;

        public void Init()
        {
        }

        /// <summary>
        /// 通知ビューの表示/非表示を切り替えます。
        /// </summary>
        public void ToggleNotifyView()
        {
            _notifyImage.SetActive( !_notifyImage.activeSelf );
        }

        /// <summary>
        /// 編集可能パラメータの内容を切り替えます
        /// </summary>
        /// <param name="index">エディットモードのインデックス値</param>
        public void SwitchEditParamView( int index )
        {
            for ( int i = 0; i < _editParams.Length; ++i )
            {
                if ( i == index ) { _editParams[index].SetActive( true ); }
                else { _editParams[i].SetActive( false ); }
            }
        }

        /// <summary>
        /// 通知ビューに表示するテキストを設定します。
        /// </summary>
        /// <param name="word">表示テキスト</param>
        public void SetNotifyWord( string word )
        {
            if ( _notifyImage != null )
            {
                _notifyImage.GetComponentInChildren<TextMeshProUGUI>().text = word;
            }
        }

        /// <summary>
        /// エディット可能パラメータのテキストを更新します。
        /// </summary>
        /// <param name="type">タイプ</param>
        /// <param name="height">高さ</param>
        public void UpdateText( StageEditMode mode, StageEditRefParams refParams )
        {
            string[] firstParamText = new string[(int)StageEditMode.NUM]
        {
            ( ( TileType )refParams.SelectedType ).ToString(),
            refParams.Col.ToString(),
            ""
        };

            string[] secondParamText = new string[(int)StageEditMode.NUM]
        {
            refParams.SelectedHeight.ToString(),
            refParams.Row.ToString(),
            ""
        };

            _editModeTextMesh.text = mode.ToString().Replace( '_', ' ' );
            _firstParamTextMesh[( int )mode].text = firstParamText[( int )mode];
            _secondParamTextMesh[( int )mode].text = secondParamText[( int )mode];
        }
    }
}

#endif // UNITY_EDITOR