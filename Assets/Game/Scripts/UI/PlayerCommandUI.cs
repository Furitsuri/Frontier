using Frontier.Combat;
using Frontier.StateMachine;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Frontier
{
    public class PlayerCommandUI : UiMonoBehaviour
    {
        [SerializeField] private GameObject _TMPCommandStringSample;
        [SerializeField] private CommandItem _commandItemSample;

        [Inject] private HierarchyBuilderBase _hierarchyBld = null;

        private List<CommandItem> _commandItems = new List<CommandItem>();
        private RectTransform _commandUIBaseRectTransform;
        private VerticalLayoutGroup _cmdTextVerticalLayout;
        private PlSelectCommandState _PlSelectScript;
        private string[] _commandTextKeys;

        /// <summary>
        /// コマンドの文字列を初期化します
        /// MEMO : コマンドが新しく増える度に文字列を追加してください
        /// </summary>
        void InitCommandStrings()
        {
            _commandTextKeys = new string[( int ) COMMAND_TAG.NUM]
            {
                "UI_CMD_MOVE",
                "UI_CMD_ATTACK",
                "UI_CMD_WAIT"
            };
            Debug.Assert( _commandTextKeys.Length == ( int ) COMMAND_TAG.NUM );
        }

        // Update is called once per frame
        void Update()
        {
            UpdateSelectCommand();
        }

        /// <summary>
        /// プレイヤーコマンドの更新処理を行います
        /// </summary>
        void UpdateSelectCommand()
        {
            // 一度全てを白色に設定
            foreach( var command in _commandItems )
            {
                command.SetColor( Color.white );
            }

            // 選択項目を赤色に設定
            _commandItems[_PlSelectScript.GetCommandIndex()].SetColor( Color.red );
        }

        /// <summary>
        /// プレイヤーコマンドの選択UIの下地となるRectTransformの大きさを更新します
        /// </summary>
        /// <param name="PLCommands">プレイヤーのコマンド構造体配列</param>
        void ResizeUIBaseRectTransform( float fontSize, int executableCmdNum )
        {
            float marginSize = _cmdTextVerticalLayout.padding.top * 2f;  // 上下のマージンサイズが存在するため2倍の値
            float intervalSize = _cmdTextVerticalLayout.spacing;

            _commandUIBaseRectTransform.sizeDelta = new Vector2( _commandUIBaseRectTransform.sizeDelta.x, marginSize + ( fontSize + intervalSize ) * executableCmdNum - intervalSize );
        }

        /// <summary>
        /// プレイヤーコマンドのスクリプトを登録します
        /// </summary>
        /// <param name="script">プレイヤーコマンドのスクリプト</param>
        public void RegistPLCommandScript( PlSelectCommandState script )
        {
            _PlSelectScript = script;
        }

        /// <summary>
        /// 実行可能なコマンドをコマンドリストUIに設定します
        /// </summary>
        /// <param name="executableCommands">実行可能なコマンドリスト</param>
        public void SetExecutableCommandList( in List<COMMAND_TAG> executableCommands )
        {
            float fontSize = 0;

            foreach( var command in _commandItems )
            {
                Destroy( command.gameObject );
            }
            _commandItems.Clear();

            // 実行可能なコマンドの文字列をリストに追加し、そのゲームオブジェクトを子として登録
            for( int i = 0; i < executableCommands.Count; ++i )
            {
                CommandItem commandItem = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<CommandItem>( _commandItemSample.gameObject, true, false, "command_" + i );
                commandItem.Setup();
                commandItem.transform.SetParent( this.gameObject.transform, false );
                commandItem.SetTextKey( _commandTextKeys[( int ) executableCommands[i]] );
                _commandItems.Add( commandItem );

                fontSize = commandItem.GetFontSize();
            }

            // 選択可能なコマンド数を用いてUIの下地の大きさを変更
            ResizeUIBaseRectTransform( fontSize, executableCommands.Count );
        }

        override public void Setup()
        {
            base.Setup();

            Debug.Assert( _hierarchyBld != null, "HierarchyBuilderBaseのインスタンスが生成されていません。Injectの設定を確認してください。" );

            _commandUIBaseRectTransform     = gameObject.GetComponent<RectTransform>();
            _cmdTextVerticalLayout          = gameObject.GetComponent<VerticalLayoutGroup>();
            TextMeshProUGUI[] commandNames  = gameObject.GetComponentsInChildren<TextMeshProUGUI>();

            InitCommandStrings();   // コマンド文字列を初期化
        }
    }
}