using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

namespace Frontier
{
    [CustomEditor(typeof(DebugBattleRoutineController))]
    public class DebugBattleLoaderEditor : Editor
    {
        SerializedProperty _battleMgr;
        SerializedProperty _propParamUnit;

        private void OnEnable()
        {
            _battleMgr = serializedObject.FindProperty("_btlRtnCtrl");
            _propParamUnit = serializedObject.FindProperty("GenerateUnitParamSetting");
        }

        override public void OnInspectorGUI()
        {
            serializedObject.Update();

            // 生成するユニットのパラメータ設定
            EditorGUILayout.PropertyField(_battleMgr);

            // Inspector上の仕切り線を表示
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(10);

            // 戦闘デバッグ用jsonデータの読込ボタン
            if (GUILayout.Button("Load Battle Debug Unit Data"))
            {
                LoadBattleDebugUnitDatas();
            }

            // 生成するユニットのパラメータ設定
            EditorGUILayout.PropertyField(_propParamUnit);

            // パラメータ設定を行ったユニットの生成ボタン
            if (GUILayout.Button("Generate Unit"))
            {
                GenerateUnit();
            }

            serializedObject.ApplyModifiedProperties();
        }

        public void LoadBattleDebugUnitDatas()
        {
            Debug.Log("Battle Debug Datas Loaded.");
        }


        public void GenerateUnit()
        {
            /*
            int prefabIndex = Params[i].Prefab;
            GameObject playerObject = Instantiate(PlayersPrefab[prefabIndex]);
            if (playerObject == null) continue;

            Player player = playerObject.GetComponent<Player>();
            if (player == null) continue;

            // ファイルから読み込んだパラメータを設定
            ApplyCharacterParams(ref player.param, Params[i]);
            player.Init();
            playerObject.SetActive(true);

            BattleRoutineController.Instance.AddPlayerToList(player);

            Debug.Log("Unit Generated.");
            */
        }
    }
}