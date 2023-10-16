using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Frontier
{
    public class PlayerCommandUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject _TMPCommandStringSample;

        private List<TextMeshProUGUI> _commandTexts = new List<TextMeshProUGUI>();
        private RectTransform _commandUIBaseRectTransform;
        private PLSelectCommandState _PLSelectScript;
        private string[] _commandStrings;

        void Awake()
        {
            _commandUIBaseRectTransform     = gameObject.GetComponent<RectTransform>();
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
            _commandStrings = new string[(int)Character.Command.COMMAND_TAG.NUM]
            {
                "MOVE",
                "ATTACK",
                "WAIT"
            };

            Debug.Assert( _commandStrings.Length == (int)Character.Command.COMMAND_TAG.NUM );
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
            _commandTexts[_PLSelectScript.SelectCommandIndex].color = Color.red;
        }

        /// <summary>
        /// プレイヤーコマンドの選択UIの下地となるRectTransformの大きさを更新します
        /// </summary>
        /// <param name="PLCommands">プレイヤーのコマンド構造体配列</param>
        void ResizeUIBaseRectTransform( float fontSize, int executableCmdNum )
        {
            const float marginSize      = 20f;  // 上下のマージンサイズがそれぞれ10fであるため2倍の値
            const float intervalSize    = 10f;

            _commandUIBaseRectTransform.sizeDelta = new Vector2(_commandUIBaseRectTransform.sizeDelta.x, marginSize + (fontSize + intervalSize ) * executableCmdNum - intervalSize);
        }

        /// <summary>
        /// プレイヤーコマンドのスクリプトを登録します
        /// </summary>
        /// <param name="script">プレイヤーコマンドのスクリプト</param>
        public void RegistPLCommandScript(Frontier.PLSelectCommandState script)
        {
            _PLSelectScript = script;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="executables"></param>
        public void SetExecutableCommandList( in List<Character.Command.COMMAND_TAG> executableCommands )
        {
            float fontSize = 0;
            const float marjin = 10f;

            foreach ( var cmdText in _commandTexts )
            {
                Destroy(cmdText.gameObject);
            }
            _commandTexts.Clear();

            // 実行可能なコマンドの文字列をリストに追加し、そのゲームオブジェクトを子として登録
            for (int i = 0; i < executableCommands.Count; ++i)
            {
                GameObject stringObject = Instantiate(_TMPCommandStringSample);
                if (stringObject == null) continue;
                TextMeshProUGUI commandString = stringObject.GetComponent<TextMeshProUGUI>();
                commandString.transform.SetParent(this.gameObject.transform);
                commandString.SetText(_commandStrings[(int)executableCommands[i]]);
                commandString.rectTransform.anchoredPosition = new Vector2(0f, -marjin - 30f * i);
                commandString.gameObject.SetActive( true );
                _commandTexts.Add(commandString);
                fontSize = commandString.fontSize;
            }

            // 選択可能なコマンド数を用いてUIの下地の大きさを変更
            ResizeUIBaseRectTransform(fontSize, executableCommands.Count);
        }
    }
}