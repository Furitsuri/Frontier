using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditorInternal;
using UnityEngine;

namespace Frontier.DebugTools.StageEditor
{
    public class EditParamPresenter : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _editModeTextMesh;
        [SerializeField] private TextMeshProUGUI _tileTypeTextMesh;
        [SerializeField] private TextMeshProUGUI _heightTextMesh;

        public void Link(int type, int height)
        {
            _tileTypeTextMesh.text = type.ToString();
        }

        /// <summary>
        /// テキストを更新します。
        /// </summary>
        /// <param name="type">タイプ</param>
        /// <param name="height">高さ</param>
        public void UpdateText(StageEditMode mode, int type, float height)
        {
            _editModeTextMesh.text   = mode.ToString().Replace('_', ' ');
            _tileTypeTextMesh.text   = ((TileType)type).ToString();
            _heightTextMesh.text     = height.ToString();
        }
    }
}