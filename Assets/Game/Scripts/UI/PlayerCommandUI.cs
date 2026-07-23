using Frontier.Combat;
using Frontier.Battle;
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
        private float _defaultWidth; // Setup()時点の幅。表示するコマンドの文字列がこれより狭い場合はこの幅を下限として使う
        private ICommandCursorProvider _activeCommandScript;
        private string[] _commandTextKeys;
        private string[] _useSkillOptionTextKeys;
        private string[] _reservedActionOptionTextKeys;
        private string[] _tileMenuOptionTextKeys;

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
                "UI_CMD_SKILL",
                "UI_CMD_WAIT"
            };
            Debug.Assert( _commandTextKeys.Length == ( int ) COMMAND_TAG.NUM );

            _useSkillOptionTextKeys = new string[( int ) USE_SKILL_OPTION_TAG.NUM]
            {
                "UI_CMD_USE_SKILL_OPTION_EXECUTION",    // EXECUTION
                "UI_CMD_USE_SKILL_OPTION_QUEUE",        // QUEUE
                "UI_CMD_USE_SKILL_OPTION_COOPERATIVE",  // COOPERATIVE
            };
            Debug.Assert( _useSkillOptionTextKeys.Length == ( int ) USE_SKILL_OPTION_TAG.NUM );

            _reservedActionOptionTextKeys = new string[( int ) RESERVED_ACTION_OPTION_TAG.NUM]
            {
                "UI_CMD_RESERVED_ACTION_EXECUTE",  // EXECUTE
            };
            Debug.Assert( _reservedActionOptionTextKeys.Length == ( int ) RESERVED_ACTION_OPTION_TAG.NUM );

            _tileMenuOptionTextKeys = new string[( int ) TILE_MENU_OPTION_TAG.NUM]
            {
                "UI_CMD_OPTION",    // OPTION
                "UI_CMD_TURN_END",  // TURN_END
            };
            Debug.Assert( _tileMenuOptionTextKeys.Length == ( int ) TILE_MENU_OPTION_TAG.NUM );
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
            if( _commandItems.Count == 0 ) { return; }

            // 一度全てを白色に設定
            foreach( var command in _commandItems )
            {
                command.SetColor( Color.white );
            }

            // 選択項目を赤色に設定
            if( _activeCommandScript != null )
            {
                _commandItems[_activeCommandScript.GetCurrentIndex()].SetColor( Color.red );
            }
        }

        /// <summary>
        /// プレイヤーコマンドの選択UIの下地となるRectTransformの大きさを更新します。
        /// 幅は _commandItems の中で最も長い文字列に合わせて自動調整します(_defaultWidthを下限とする)。
        /// </summary>
        /// <param name="PLCommands">プレイヤーのコマンド構造体配列</param>
        void ResizeUIBaseRectTransform( float fontSize, int executableCmdNum )
        {
            float marginSize   = _cmdTextVerticalLayout.padding.top * 2f;  // 上下のマージンサイズが存在するため2倍の値
            float intervalSize = _cmdTextVerticalLayout.spacing;
            float horizontalMarginSize = _cmdTextVerticalLayout.padding.left + _cmdTextVerticalLayout.padding.right;

            float maxTextWidth = 0f;
            foreach( var item in _commandItems )
            {
                maxTextWidth = Mathf.Max( maxTextWidth, item.GetPreferredWidth() );
            }
            float width = Mathf.Max( _defaultWidth, maxTextWidth + horizontalMarginSize );

            _commandUIBaseRectTransform.sizeDelta = new Vector2( width, marginSize + ( fontSize + intervalSize ) * executableCmdNum - intervalSize );
        }

        /// <summary>
        /// カーソル位置を提供するスクリプトを登録します。nullを渡すと登録を解除します。
        /// </summary>
        /// <param name="script">登録するスクリプト</param>
        public void RegistCommandScript( ICommandCursorProvider script )
        {
            _activeCommandScript = script;
        }

        /// <summary>
        /// スキル使用オプションのリストをUIに設定します。
        /// 表示する選択肢を options で指定します。
        /// </summary>
        public void SetUseSkillOptionList( List<USE_SKILL_OPTION_TAG> options )
        {
            float fontSize = 0;

            foreach( var command in _commandItems )
            {
                Destroy( command.gameObject );
            }
            _commandItems.Clear();

            for( int i = 0; i < options.Count; ++i )
            {
                CommandItem commandItem = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<CommandItem>( _commandItemSample.gameObject, true, false, "option_" + i );
                commandItem.Setup();
                commandItem.transform.SetParent( this.gameObject.transform, false );
                commandItem.SetTextKey( _useSkillOptionTextKeys[( int ) options[i]] );
                _commandItems.Add( commandItem );

                fontSize = commandItem.GetFontSize();
            }

            ResizeUIBaseRectTransform( fontSize, options.Count );
        }

        /// <summary>
        /// スキル予約に対する操作の選択肢リストをUIに設定します。
        /// 表示する選択肢を options で指定します。
        /// </summary>
        public void SetReservedActionOptionList( List<RESERVED_ACTION_OPTION_TAG> options )
        {
            float fontSize = 0;

            foreach( var command in _commandItems )
            {
                Destroy( command.gameObject );
            }
            _commandItems.Clear();

            for( int i = 0; i < options.Count; ++i )
            {
                CommandItem commandItem = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<CommandItem>( _commandItemSample.gameObject, true, false, "option_" + i );
                commandItem.Setup();
                commandItem.transform.SetParent( this.gameObject.transform, false );
                commandItem.SetTextKey( _reservedActionOptionTextKeys[( int ) options[i]] );
                _commandItems.Add( commandItem );

                fontSize = commandItem.GetFontSize();
            }

            ResizeUIBaseRectTransform( fontSize, options.Count );
        }

        /// <summary>
        /// タイルメニュー(Option/Turn End)の選択肢リストをUIに設定します。
        /// </summary>
        public void SetTileMenuOptionList( List<TILE_MENU_OPTION_TAG> options )
        {
            float fontSize = 0;

            foreach( var command in _commandItems )
            {
                Destroy( command.gameObject );
            }
            _commandItems.Clear();

            for( int i = 0; i < options.Count; ++i )
            {
                CommandItem commandItem = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<CommandItem>( _commandItemSample.gameObject, true, false, "option_" + i );
                commandItem.Setup();
                commandItem.transform.SetParent( this.gameObject.transform, false );
                commandItem.SetTextKey( _tileMenuOptionTextKeys[( int ) options[i]] );
                _commandItems.Add( commandItem );

                fontSize = commandItem.GetFontSize();
            }

            ResizeUIBaseRectTransform( fontSize, options.Count );
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

        public override void Setup()
        {
            base.Setup();

            Debug.Assert( _hierarchyBld != null, "HierarchyBuilderBaseのインスタンスが生成されていません。Injectの設定を確認してください。" );

            _commandUIBaseRectTransform     = gameObject.GetComponent<RectTransform>();
            _cmdTextVerticalLayout          = gameObject.GetComponent<VerticalLayoutGroup>();
            TextMeshProUGUI[] commandNames  = gameObject.GetComponentsInChildren<TextMeshProUGUI>();
            _defaultWidth                   = _commandUIBaseRectTransform.sizeDelta.x;

            // childControlWidthがfalseだと、下地(_commandUIBaseRectTransform)の幅を変更しても
            // 各CommandItemの幅は追従しないため、テキストが折り返されたままになってしまう。
            // trueにすることでchildForceExpandWidthと合わせて下地の幅に子要素の幅が追従するようにする
            _cmdTextVerticalLayout.childControlWidth = true;

            InitCommandStrings();   // コマンド文字列を初期化
        }
    }
}
