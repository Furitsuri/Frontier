using Frontier.Stage;
using System;
using System.IO;
using TMPro;
using UnityEngine;
using static Frontier.DebugTools.StageEditor.StageEditorController;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    public class StageEditorPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject _editParamImage;
        [SerializeField] private GameObject[] _editParams;                  // エディット用のパラメータ群
        [SerializeField] private ConfirmUI _confirmSaveLoadUI;              // 保存・読込確認用UI
        [SerializeField] private StageEditorEditFileNameUI _editFileNameUI; // ファイル名編集用UI
        [SerializeField] private FileSuggestor _fileNameSuggestor;
        [SerializeField] private TextMeshProUGUI _fileNameTextMesh;
        [SerializeField] private TextMeshProUGUI _editModeTextMesh;
        [SerializeField] private TextMeshProUGUI[] _firstParamTextMesh;
        [SerializeField] private TextMeshProUGUI[] _secondParamTextMesh;

        private Holder<string> _holdEditFileName;

        public void Init( Holder<string> editFileName )
        {
            _confirmSaveLoadUI.Init();
            _editFileNameUI.Init();
            _fileNameSuggestor.Init( () =>
            {
                // タブキーが押された場合、候補の最上位を入力フィールドにセットする
                var topMostSuggestion = _fileNameSuggestor.GetTopMostSuggestion();
                if( topMostSuggestion != null )
                {
                    var tmp = topMostSuggestion.GetComponentInChildren<TextMeshProUGUI>();
                    if( null != tmp )
                    {
                        // 拡張子を削除した文字列を入力中のInputFieldに代入
                        _editFileNameUI.SetInputFiledText( Path.GetFileNameWithoutExtension( tmp.text ) );
                    }
                }
            } );

            _holdEditFileName = editFileName;
            _fileNameTextMesh.text = _holdEditFileName.Value;
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

        public void OpenEditFileName( Action OnComplete )
        {
            _fileNameTextMesh.gameObject.SetActive( false );
            _fileNameSuggestor.StartSuggest();

            _editFileNameUI.Open( _fileNameTextMesh.text,
                ( filename ) => 
                {
                    _fileNameTextMesh.text =_holdEditFileName.Value = filename;
                },
                () =>
                {
                    OnComplete?.Invoke();
                    _fileNameSuggestor.EndSuggest();
                }
            );
        }

        public void CloseEditFileName()
        {
            _fileNameTextMesh.gameObject.SetActive( true );
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

        public bool HasSuggestions()
        {
            return _fileNameSuggestor.GetTopMostSuggestion() != null;
        }

        public ConfirmUI GetConfirmSaveLoadUI()
        {
            return _confirmSaveLoadUI;
        }
    }
}

#endif // UNITY_EDITOR