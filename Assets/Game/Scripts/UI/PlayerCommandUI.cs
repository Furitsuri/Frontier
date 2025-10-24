using Frontier.Combat;
using Frontier.StateMachine;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Frontier
{
    public class PlayerCommandUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject _TMPCommandStringSample;

        [Inject]
        private HierarchyBuilderBase _hierarchyBld = null;

        private List<TextMeshProUGUI> _commandTexts = new List<TextMeshProUGUI>();
        private RectTransform _commandUIBaseRectTransform;
        private VerticalLayoutGroup _cmdTextVerticalLayout;
        private PlSelectCommandState _PlSelectScript;
        private string[] _commandStrings;

        void Awake()
        {
            Debug.Assert(_hierarchyBld != null, "HierarchyBuilderBaseのインスタンスが生成されていません。Injectの設定を確認してください。");

            _commandUIBaseRectTransform     = gameObject.GetComponent<RectTransform>();
            _cmdTextVerticalLayout          = gameObject.GetComponent<VerticalLayoutGroup>();
            TextMeshProUGUI[] commandNames  = gameObject.GetComponentsInChildren<TextMeshProUGUI>();

            // コマンド文字列を初期化
            InitCommandStrings();

            // 起動直後にActiveをOffに
            this.gameObject.SetActive(false);
        }

        /// <summary>
        /// コマンドの文字列を初期化します
        /// MEMO : コマンドが新しく増える度に文字列を追加してください
        /// </summary>
        void InitCommandStrings()
        {
            _commandStrings = new string[(int)COMMAND_TAG.NUM]
            {
                "MOVE",
                "ATTACK",
                "WAIT"
            };

            Debug.Assert( _commandStrings.Length == (int)COMMAND_TAG.NUM );
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
            foreach (var text in _commandTexts)
            {
                text.color = Color.white;
            }

            // 選択項目を赤色に設定
            _commandTexts[_PlSelectScript.GetCommandIndex()].color = Color.red;
        }

        /// <summary>
        /// プレイヤーコマンドの選択UIの下地となるRectTransformの大きさを更新します
        /// </summary>
        /// <param name="PLCommands">プレイヤーのコマンド構造体配列</param>
        void ResizeUIBaseRectTransform( float fontSize, int executableCmdNum )
        {
            float marginSize      = _cmdTextVerticalLayout.padding.top * 2f;  // 上下のマージンサイズが存在するため2倍の値
            float intervalSize    = _cmdTextVerticalLayout.spacing;

            _commandUIBaseRectTransform.sizeDelta = new Vector2(_commandUIBaseRectTransform.sizeDelta.x, marginSize + (fontSize + intervalSize ) * executableCmdNum - intervalSize);
        }

        /// <summary>
        /// プレイヤーコマンドのスクリプトを登録します
        /// </summary>
        /// <param name="script">プレイヤーコマンドのスクリプト</param>
        public void RegistPLCommandScript( PlSelectCommandState script)
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

            foreach ( var cmdText in _commandTexts )
            {
                Destroy(cmdText.gameObject);
            }
            _commandTexts.Clear();

            // 実行可能なコマンドの文字列をリストに追加し、そのゲームオブジェクトを子として登録
            for (int i = 0; i < executableCommands.Count; ++i)
            {
                TextMeshProUGUI commandString = _hierarchyBld.CreateComponentAndOrganize<TextMeshProUGUI>(_TMPCommandStringSample, true);
                commandString.transform.SetParent(this.gameObject.transform, false);
                commandString.SetText(_commandStrings[(int)executableCommands[i]]);
                _commandTexts.Add(commandString);
                fontSize = commandString.fontSize;
            }

            // 選択可能なコマンド数を用いてUIの下地の大きさを変更
            ResizeUIBaseRectTransform(fontSize, executableCommands.Count);
        }
    }
}