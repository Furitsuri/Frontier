using Frontier.Stage;
using TMPro;
using UnityEngine;
using static Frontier.DebugTools.StageEditor.StageEditorController;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    public class StageEditorPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject _notifyImage;                   // 通知用画像オブジェクト
        [SerializeField] private GameObject _editParamImage;
        [SerializeField] private GameObject[] _editParams;                  // エディット用のパラメータ群
        [SerializeField] private ConfirmUI _confirmSaveLoadUI;              // 保存・読込確認用UI
        [SerializeField] private TextMeshProUGUI _editModeTextMesh;
        [SerializeField] private TextMeshProUGUI[] _firstParamTextMesh;
        [SerializeField] private TextMeshProUGUI[] _secondParamTextMesh;

        public void Init()
        {
            _confirmSaveLoadUI.Init();
        }

        /// <summary>
        /// 編集可能パラメータの内容を切り替えます
        /// </summary>
        /// <param name="index">エディットモードのインデックス値</param>
        public void SwitchEditParamView( StageEditMode mode )
        {
            // キャラクター配置エディットでは、編集可能パラメータが存在しないため表示しない
            _editParamImage.SetActive( ( mode != StageEditMode.EDIT_CHARACTER_DEPLOYMENT_TILE ) );

            int index = ( int ) mode;

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
        public void SetMessageWord( string word )
        {
            if ( _confirmSaveLoadUI != null )
            {
                _confirmSaveLoadUI.SetMessageText( word );
            }
        }

        /// <summary>
        /// 確認ウィンドウのサイズを更新します。
        /// </summary>
        /// <param name="newSize"></param>
        public void RefreshConfirmWindowSize( in Vector2 newSize )
        {
            _confirmSaveLoadUI.GetComponent<RectTransform>().sizeDelta = newSize;
        }

        /// <summary>
        /// エディット可能パラメータのテキストを更新します。
        /// </summary>
        /// <param name="type">タイプ</param>
        /// <param name="height">高さ</param>
        public void UpdateModeText( StageEditMode mode, StageEditRefParams refParams )
        {
            string[] firstParamText = new string[( int ) StageEditMode.NUM]
            {
                ( ( TileType )refParams.SelectedType ).ToString(),
                refParams.Col.ToString(),
                ""
            };

                string[] secondParamText = new string[( int ) StageEditMode.NUM]
            {
                refParams.SelectedHeight.ToString(),
                refParams.Row.ToString(),
                ""
            };

            _editModeTextMesh.text = mode.ToString().Replace( '_', ' ' );

            // キャラクターの配置タイル設定編集では、エディット可能パラメータが存在しないため終了する
            if( mode == StageEditMode.EDIT_CHARACTER_DEPLOYMENT_TILE ) { return; }

            _firstParamTextMesh[( int ) mode].text  = firstParamText[( int ) mode];
            _secondParamTextMesh[( int ) mode].text = secondParamText[( int ) mode];
        }

        public ConfirmUI GetConfirmSaveLoadUI()
        {
            return _confirmSaveLoadUI;
        }
    }
}

#endif // UNITY_EDITOR