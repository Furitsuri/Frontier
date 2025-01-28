using UnityEngine;
using System;
using System.IO;
using TMPro.SpriteAssetUtilities;
using Palmmedia.ReportGenerator.Core.Common;
using System.Collections.Generic;
using Newtonsoft.Json;
using Frontier.Stage;
using Zenject;
using static UnityEngine.EventSystems.EventTrigger;

namespace Frontier
{
    public class BattleFileLoader : MonoBehaviour
    {
        [Header("�e�����L�����N�^�[�̃v���n�u")]
        [SerializeField]
        public GameObject[] PlayersPrefab;

        [Header("�e�G�L�����N�^�[�̃v���n�u")]
        [SerializeField]
        public GameObject[] EnemiesPrefab;

        [Header("�e��O���̓L�����N�^�[�̃v���n�u")]
        [SerializeField]
        public GameObject[] OthersPrefab;

        [Header("�e�����L�����N�^�[�̃p�����[�^�Q�Ɛ�")]
        [SerializeField]
        public string[] PlayerParamFilePath;

        [Header("�e�G�L�����N�^�[�̃p�����[�^�Q�Ɛ�")]
        [SerializeField]
        public string[] EnemyParamFilePath;

        [Header("�e��O�R���L�����N�^�[�̃p�����[�^�Q�Ɛ�")]
        [SerializeField]
        public string[] OtherParamFilePath;

        [Header("�e�X�L���f�[�^�̃p�����[�^�Q�Ɛ�")]
        [SerializeField]
        public string SkillDataFilePath;

        [Header("�ߐڍU�����̃J�����p�����[�^�̎Q�Ɛ�")]
        [SerializeField]
        public string CloseAtkCameraParamFilePath;

        [Header("���u�U�����̃J�����p�����[�^�̎Q�Ɛ�")]
        [SerializeField]
        public string RangedAtkCameraParamFilePath;

        private HierarchyBuilder _hierarchyBld = null;

        // �o�g���}�l�[�W��
        private BattleManager _btlMgr;

        [System.Serializable]
        public struct CharacterParamData
        {
            public int CharacterTag;
            public int CharacterIndex;
            public int MaxHP;
            public int Atk;
            public int Def;
            public int MoveRange;
            public int AtkRange;
            public int ActGaugeMax;
            public int ActRecovery;
            public int InitGridIndex;
            public int InitDir;
            public int Prefab;
            public int ThinkType;
            public int[] Skills;
        }

        [System.Serializable]
        public struct FileSkillData
        {
            public string Name;
            public int Cost;
            public int Type;
            public int Duration;
            public float AddAtkMag;
            public float AddDefMag;
            public int AddAtkNum;
            public float Param1;
            public float Param2;
            public float Param3;
            public float Param4;
        }

        [System.Serializable]
        public class PlayerParamContainer
        {
            public CharacterParamData[] CharacterParams;
        }

        [System.Serializable]
        public class CharacterParamContainer
        {
            public CharacterParamData[] CharacterParams;
        }

        [System.Serializable]
        public class SkillDataContainer
        {
            public FileSkillData[] SkillsData;
        }

        [System.Serializable]
        public class CameraParamContainer
        {
            public List<BattleCameraController.CameraParamData[]> CameraParams;
        }

        /// <summary>
        /// Di�R���e�i��������𒍓����܂�
        /// </summary>
        /// <param name="btlMgr"></param>
        /// <param name="hierarchyBld"></param>
        [Inject]
        void Construct(BattleManager btlMgr, HierarchyBuilder hierarchyBld)
        {
            _btlMgr         = btlMgr;
            _hierarchyBld   = hierarchyBld;
        }

        /// <summary>
        /// �Y���X�e�[�W�̑S�L�����N�^�[�������[�h���A�o�g���}�l�[�W����ɐݒu���܂�
        /// </summary>
        /// <param name="stageIndex">�X�e�[�W�i���o�[</param>
        public void CharacterLoad(int stageIndex)
        {
            List<string>[] ParamFilePaths = new List<string>[]
            {
                new List<string>(PlayerParamFilePath),
                new List<string>(EnemyParamFilePath),
                new List<string>(OtherParamFilePath),
            };

            List<GameObject>[] CharacterPrefabs = new List<GameObject>[]
            {
                new List<GameObject>(PlayersPrefab),
                new List<GameObject>(EnemiesPrefab),
                new List<GameObject>(OthersPrefab),
            };

            for (int i = 0; i < (int)Character.CHARACTER_TAG.NUM; ++i)
            {
                if ( ParamFilePaths[i].Count <= 0 ) continue;

                // JSON�t�@�C���̓ǂݍ���
                string json = File.ReadAllText(ParamFilePaths[i][stageIndex]);
                // JSON�f�[�^�̃f�V���A���C�Y
                var dataContainer = JsonUtility.FromJson<CharacterParamContainer>(json);
                if (dataContainer == null) return;
                
                // �f�V���A���C�Y���ꂽ�f�[�^��z��Ɋi�[
                foreach ( var param in dataContainer.CharacterParams )
                {
                    int prefabIndex = param.Prefab;
                    Character chara = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<Character>(CharacterPrefabs[i][prefabIndex], true, false);

                    // �e�I�u�W�F�N�g���ݒ肳��Ă���ΐ���
                    // �g�p���܂Ŕ�A�N�e�B�u�ɂ���
                    if (chara.BulletObject != null)
                    {
                        _hierarchyBld.CreateComponentNestedNewDirectoryWithDiContainer<Bullet>(chara.BulletObject, chara.gameObject, "Bullet", false, false);
                    }

                    // �t�@�C������ǂݍ��񂾃p�����[�^��ݒ�
                    ApplyCharacterParams(ref chara.param, param);
                    chara.Init();
                    chara.SetThinkType((Character.ThinkingType)param.ThinkType);

                    _btlMgr.BtlCharaCdr.AddCharacterToList(chara);

                }
            }
        }

        /// <summary>
        /// �e�X�L���̃f�[�^�����[�h���܂�
        /// </summary>
        public void SkillDataLord()
        {
            string json = File.ReadAllText(SkillDataFilePath);
            var dataContainer = JsonUtility.FromJson<SkillDataContainer>(json);
            if (dataContainer == null) return;
            for (int i = 0; i < (int)SkillsData.ID.SKILL_NUM; ++i)
            {
                ApplySkillsData(ref SkillsData.data[i], dataContainer.SkillsData[i]);
            }
        }

        /// <summary>
        /// �J�����̃p�����[�^��ǂݍ��݂܂�
        /// </summary>
        public void CameraParamLord( BattleCameraController cameraController )
        {
            string json = File.ReadAllText(CloseAtkCameraParamFilePath);
            var dataContainer = JsonConvert.DeserializeObject<CameraParamContainer>(json);
            if (dataContainer == null) return;
            List<BattleCameraController.CameraParamData[]> closeParams = dataContainer.CameraParams;

            json = File.ReadAllText(RangedAtkCameraParamFilePath);
            dataContainer = JsonConvert.DeserializeObject<CameraParamContainer>(json);
            if (dataContainer == null) return;
            List<BattleCameraController.CameraParamData[]> rangedParams = dataContainer.CameraParams;

            cameraController.SetCameraParamDatas(closeParams, rangedParams);
        }

        /// <summary>
        /// �L�����N�^�[�p�����[�^��K�������܂�
        /// </summary>
        /// <param name="param">�K����̃L�����N�^�[�p�����[�^</param>
        /// <param name="fdata">�K�����̃L�����N�^�[�p�����[�^</param>
        private void ApplyCharacterParams(ref Character.Parameter param, in CharacterParamData fdata)
        {
            param.characterTag = (Character.CHARACTER_TAG)fdata.CharacterTag;
            param.characterIndex = fdata.CharacterIndex;
            param.CurHP = param.MaxHP = fdata.MaxHP;
            param.Atk = fdata.Atk;
            param.Def = fdata.Def;
            param.moveRange = fdata.MoveRange;
            param.attackRange = fdata.AtkRange;
            param.curActionGauge = param.maxActionGauge = fdata.ActGaugeMax;
            param.recoveryActionGauge = fdata.ActRecovery;
            param.initGridIndex = fdata.InitGridIndex;
            param.initDir = (Constants.Direction)fdata.InitDir;
            for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i)
            {
                param.equipSkills[i] = (SkillsData.ID)fdata.Skills[i];
            }
        }

        /// <summary>
        /// �X�L���f�[�^��K�������܂�
        /// </summary>
        /// <param name="data">�K����̃X�L���f�[�^</param>
        /// <param name="fdata">�K�����̃t�@�C������ǂݎ�����X�L���f�[�^</param>
        private void ApplySkillsData(ref SkillsData.Data data, in FileSkillData fdata)
        {
            data.Name = fdata.Name;
            data.Cost = fdata.Cost;
            data.Type = (SkillsData.SituationType)fdata.Type;
            data.Duration = fdata.Duration;
            data.AddAtkMag = fdata.AddAtkMag;
            data.AddDefMag = fdata.AddDefMag;
            data.AddAtkNum = fdata.AddAtkNum;
            data.Param1 = fdata.Param1;
            data.Param2 = fdata.Param2;
            data.Param3 = fdata.Param3;
            data.Param4 = fdata.Param4;
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        /// <summary>
        /// �f�o�b�O�p�Ƀ��j�b�g�𐶐����܂�
        /// </summary>
        public void DebugBattleLoadUnit(int prefabIndex, ref Character.Parameter param)
        {
            if (param.characterTag == Character.CHARACTER_TAG.PLAYER)
            {
                Player player = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<Player>(PlayersPrefab[prefabIndex], true, false);
                if (player == null) return;

                player.param = param;
                player.Init();

                _btlMgr.BtlCharaCdr.AddCharacterToList(player);
            }
            else
            {
                Enemy enemy = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<Enemy>(PlayersPrefab[prefabIndex], true, false);
                if (enemy == null) return;

                enemy.param = param;
                enemy.Init();

                _btlMgr.BtlCharaCdr.AddCharacterToList(enemy);
            }
        }
#endif  // DEVELOPMENT_BUILD || UNITY_EDITOR
            }
}