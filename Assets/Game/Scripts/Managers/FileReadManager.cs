using UnityEngine;
using System;
using System.IO;
using TMPro.SpriteAssetUtilities;
using Palmmedia.ReportGenerator.Core.Common;
using System.Collections.Generic;
using Newtonsoft.Json;
using Frontier.Stage;
using Zenject;

namespace Frontier
{
    public class FileReadManager : MonoBehaviour
    {
        [Header("�e�����L�����N�^�[�̃v���n�u")]
        [SerializeField]
        public GameObject[] PlayersPrefab;

        [Header("�e�G�L�����N�^�[�̃v���n�u")]
        [SerializeField]
        public GameObject[] EnemiesPrefab;

        [Header("�e�����L�����N�^�[�̃p�����[�^�Q�Ɛ�")]
        [SerializeField]
        public string[] PlayerParamFilePath;

        [Header("�e�G�L�����N�^�[�̃p�����[�^�Q�Ɛ�")]
        [SerializeField]
        public string[] EnemyParamFilePath;

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
        public struct NpcParamData
        {
            public CharacterParamData Param;
            public int ThinkType;
        }

        [System.Serializable]
        public class PlayerParamContainer
        {
            public CharacterParamData[] CharacterParams;
        }

        [System.Serializable]
        public class NpcParamContainer
        {
            public NpcParamData[] CharacterParams;
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

        void Awake()
        {
            Debug.Assert(_hierarchyBld != null, "HierarchyBuilder�̃C���X�^���X����������Ă��܂���BInject�̐ݒ���m�F���Ă��������B");
        }

        /// <summary>
        /// �v���C���[�������[�h���A�o�g���}�l�[�W����ɐݒu���܂�
        /// </summary>
        /// <param name="stageIndex">�X�e�[�W�i���o�[</param>
        public void PlayerLoad(int stageIndex, float gridLength)
        {
            // JSON�t�@�C���̓ǂݍ���
            string json = File.ReadAllText(PlayerParamFilePath[stageIndex]);
            // JSON�f�[�^�̃f�V���A���C�Y
            var dataContainer = JsonUtility.FromJson<PlayerParamContainer>(json);
            if (dataContainer == null) return;
            // �f�V���A���C�Y���ꂽ�f�[�^��z��Ɋi�[
            CharacterParamData[] Params = dataContainer.CharacterParams;

            for (int i = 0; i < Params.Length; ++i)
            {
                int prefabIndex = Params[i].Prefab;
                Player player = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<Player>(PlayersPrefab[prefabIndex], true, false);

                // �e�I�u�W�F�N�g���ݒ肳��Ă���ΐ���
                // �g�p���܂Ŕ�A�N�e�B�u�ɂ���
                if ( player.BulletObject != null )
                {
                    _hierarchyBld.CreateComponentWithNestedNewDirectoryWithDiContainer<Bullet>(player.BulletObject, player.gameObject, "Bullet", false, false);
                }

                // �t�@�C������ǂݍ��񂾃p�����[�^��ݒ�
                ApplyCharacterParams(ref player.param, Params[i]);
                player.Init();

                _btlMgr.AddPlayerToList(player);
            }
        }

        /// <summary>
        /// �G�������[�h���A�o�g���}�l�[�W����ɐݒu���܂�
        /// </summary>
        /// <param name="stageIndex">�X�e�[�W�i���o�[</param>
        public void EnemyLord(int stageIndex, float gridLength)
        {
            string json = File.ReadAllText(EnemyParamFilePath[stageIndex]);
            var dataContainer = JsonUtility.FromJson<NpcParamContainer>(json);
            if (dataContainer == null) return;
            NpcParamData[] Params = dataContainer.CharacterParams;

            for (int i = 0; i < Params.Length; ++i)
            {
                int prefabIndex = Params[i].Param.Prefab;

                Enemy enemy = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<Enemy>(EnemiesPrefab[prefabIndex], true, false);

                // �t�@�C������ǂݍ��񂾃p�����[�^��ݒ�
                ApplyCharacterParams(ref enemy.param, Params[i].Param);
                enemy.Init();
                enemy.SetThinkType((Enemy.ThinkingType)Params[i].ThinkType);

                _btlMgr.AddEnemyToList(enemy);
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

                _btlMgr.AddPlayerToList(player);
            }
            else
            {
                Enemy enemy = _hierarchyBld.CreateComponentAndOrganizeWithDiContainer<Enemy>(PlayersPrefab[prefabIndex], true, false);
                if (enemy == null) return;

                enemy.param = param;
                enemy.Init();

                _btlMgr.AddEnemyToList(enemy);
            }
        }
#endif  // DEVELOPMENT_BUILD || UNITY_EDITOR
    }
}