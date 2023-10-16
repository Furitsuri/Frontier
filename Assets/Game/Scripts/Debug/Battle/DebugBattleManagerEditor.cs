using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

namespace Frontier
{
    [CustomEditor(typeof(DebugBattleManager))]
    public class DebugBattleLoaderEditor : Editor
    {
        SerializedProperty _battleMgr;
        SerializedProperty _propParamUnit;

        private void OnEnable()
        {
            _battleMgr = serializedObject.FindProperty("_btlMgr");
            _propParamUnit = serializedObject.FindProperty("GenerateUnitParamSetting");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // �������郆�j�b�g�̃p�����[�^�ݒ�
            EditorGUILayout.PropertyField(_battleMgr);

            // Inspector��̎d�؂����\��
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.Space(10);

            // �퓬�f�o�b�O�pjson�f�[�^�̓Ǎ��{�^��
            if (GUILayout.Button("Load Battle Debug Unit Data"))
            {
                LoadBattleDebugUnitDatas();
            }

            // �������郆�j�b�g�̃p�����[�^�ݒ�
            EditorGUILayout.PropertyField(_propParamUnit);

            // �p�����[�^�ݒ���s�������j�b�g�̐����{�^��
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

            // �t�@�C������ǂݍ��񂾃p�����[�^��ݒ�
            ApplyCharacterParams(ref player.param, Params[i]);
            player.Init();
            playerObject.SetActive(true);

            BattleManager.Instance.AddPlayerToList(player);

            Debug.Log("Unit Generated.");
            */
        }
    }
}