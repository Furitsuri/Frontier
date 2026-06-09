using Frontier.Stage;
using System;
using System.IO;
using TMPro;
using UnityEngine;
using static Frontier.DebugTools.StageEditor.StageEditorController;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    public class StageEditorUI : UiMonoBehaviour
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

        [Header( "敵パラメータ一覧ウィンドウ" )]
        [SerializeField] private GameObject _enemyParamListPanel;              // EnemyParamList ルート
        [SerializeField] private TextMeshProUGUI[] _enemyParamNameTexts;       // 各行の名前テキスト (11要素)
        [SerializeField] private TextMeshProUGUI[] _enemyParamValueTexts;      // 各行の値テキスト  (11要素)
        [SerializeField] private GameObject[] _enemyParamIndicators;           // 各行の選択インジケーター (11要素)

        private static readonly Color ColorParamSelected   = new Color( 1.0f, 0.95f, 0.2f, 1.0f );  // 選択中：黄
        private static readonly Color ColorParamUnselected = new Color( 0.55f, 0.55f, 0.55f, 1.0f ); // 非選択：グレー
        private static readonly Color ColorValueSelected   = new Color( 0.2f, 1.0f, 0.6f,  1.0f );  // 選択中値：緑
        private static readonly Color ColorValueUnselected = new Color( 0.35f, 0.75f, 1.0f, 1.0f );  // 非選択値：シアン

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
            _editParamImage.SetActive( true );

            int index = ( int ) mode;

            for ( int i = 0; i < _editParams.Length; ++i )
            {
                if ( i == index ) { _editParams[index].SetActive( true ); }
                else { _editParams[i].SetActive( false ); }
            }

            // 敵パラメータ一覧ウィンドウは EDIT_ENEMY モード時のみ表示
            if ( _enemyParamListPanel != null )
            {
                _enemyParamListPanel.SetActive( mode == StageEditMode.EDIT_ENEMY );
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
        /// <param name="mode">エディットモード</param>
        /// <param name="refParams">参照パラメータ</param>
        public void UpdateModeText( StageEditMode mode, StageEditRefParams refParams )
        {
            string[] firstParamText = new string[( int ) StageEditMode.NUM]
            {
                ( ( TileType )refParams.SelectedType ).ToString(),
                refParams.Col.ToString(),
                refParams.MaxDeployableUnits.ToString(),
                StageEditRefParams.EnemyParamNames[refParams.SelectedEnemyParamIndex],  // 選択中パラメータ名
            };

            string[] secondParamText = new string[( int ) StageEditMode.NUM]
            {
                refParams.SelectedHeight.ToString(),
                refParams.Row.ToString(),
                "",
                refParams.GetEnemyParamDisplayString( refParams.SelectedEnemyParamIndex ),   // 選択中パラメータ値
            };

            _editModeTextMesh.text = mode.ToString().Replace( '_', ' ' );

            int modeIndex = ( int ) mode;
            if( _firstParamTextMesh != null && modeIndex < _firstParamTextMesh.Length && _firstParamTextMesh[modeIndex] != null && 0 < firstParamText[modeIndex].Length )
            {
                _firstParamTextMesh[modeIndex].text = firstParamText[modeIndex];
            }
            if( _secondParamTextMesh != null && modeIndex < _secondParamTextMesh.Length && _secondParamTextMesh[modeIndex] != null && 0 < secondParamText[modeIndex].Length )
            {
                _secondParamTextMesh[modeIndex].text = secondParamText[modeIndex];
            }

            // EDIT_ENEMY モード中は敵パラメータ一覧もリアルタイム更新
            if ( mode == StageEditMode.EDIT_ENEMY )
            {
                UpdateEnemyParamListView( refParams );
            }
        }

        /// <summary>
        /// 敵パラメータ一覧ウィンドウの全行テキストと選択ハイライトを更新します。
        /// </summary>
        public void UpdateEnemyParamListView( StageEditRefParams refParams )
        {
            if ( _enemyParamValueTexts == null || _enemyParamNameTexts == null ) return;

            int selected = refParams.SelectedEnemyParamIndex;

            for ( int i = 0; i < StageEditRefParams.EnemyParamNames.Length; i++ )
            {
                bool isSelected = ( i == selected );

                // 値テキスト更新
                if ( i < _enemyParamValueTexts.Length && _enemyParamValueTexts[i] != null )
                {
                    _enemyParamValueTexts[i].text  = refParams.GetEnemyParamDisplayString( i );
                    _enemyParamValueTexts[i].color = isSelected ? ColorValueSelected : ColorValueUnselected;
                }

                // 名前テキストのハイライト
                if ( i < _enemyParamNameTexts.Length && _enemyParamNameTexts[i] != null )
                {
                    _enemyParamNameTexts[i].color = isSelected ? ColorParamSelected : ColorParamUnselected;
                }

                // 選択インジケーター（▶）の表示切替
                if ( i < _enemyParamIndicators.Length && _enemyParamIndicators[i] != null )
                {
                    _enemyParamIndicators[i].SetActive( isSelected );
                }
            }
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