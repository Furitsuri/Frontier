using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class CharacterParameterPresenter : MonoBehaviour
    {
        [Header("LeftWindowParam")]
        public CharacterParameterUI PlayerParameter;        // �����\���̃p�����[�^UI�E�B���h�E

        [Header("RightWindowParam")]
        public CharacterParameterUI EnemyParameter;         // �E���\���̃p�����[�^UI�E�B���h�E

        [Header("ParameterAttackDirection")]
        public ParameterAttackDirectionUI AttackDirection;  // �p�����[�^UI�ԏ�̍U��(��)������Ώۂւ̕\��

        private BattleManager _btlMgr = null;
        private StageController _stageCtrl = null;
        private Character _prevCharacter = null;

        void Start()
        {
            _btlMgr     = ManagerProvider.Instance.GetService<BattleManager>();
            _stageCtrl  = ManagerProvider.Instance.GetService<StageController>();

            // �J�����A���O���̐ݒ�
            PlayerParameter.Init(45f, CharacterParameterUI.SIDE.LEFT);
            EnemyParameter.Init(-45f, CharacterParameterUI.SIDE.RIGHT);

            PlayerParameter.gameObject.SetActive(false);
            EnemyParameter.gameObject.SetActive(false);
        }


        // Update is called once per frame
        void Update()
        {
            Character selectCharacter = _btlMgr.GetCharacterFromHashtable(_btlMgr.SelectCharacterInfo);

            var bindCharacter = _stageCtrl.GetGridCursorBindCharacter();

            // �U���ΏۑI����
            switch (_stageCtrl.GetGridCursorState())
            {
                case Stage.GridCursor.State.ATTACK:
                    Debug.Assert(bindCharacter != null);

                    BattleUISystem.Instance.ToggleEnemyParameter(true);

                    // ��ʍ\���͈ȉ��̒ʂ�
                    //   ��        �E
                    // PLAYER �� ENEMY
                    // OTHER  �� ENEMY
                    // PLAYER �� OTHER
                    if (bindCharacter.param.characterTag != Character.CHARACTER_TAG.ENEMY)
                    {
                        PlayerParameter.SetDisplayCharacter(bindCharacter);
                        EnemyParameter.SetDisplayCharacter(selectCharacter);
                    }
                    else
                    {
                        PlayerParameter.SetDisplayCharacter(selectCharacter);
                        EnemyParameter.SetDisplayCharacter(bindCharacter);
                    }
                    break;
                case Stage.GridCursor.State.MOVE:
                    Debug.Assert(bindCharacter != null);

                    PlayerParameter.SetDisplayCharacter(bindCharacter);
                    if( selectCharacter != null && selectCharacter != bindCharacter ) EnemyParameter.SetDisplayCharacter(selectCharacter);
                    BattleUISystem.Instance.ToggleEnemyParameter(selectCharacter != null && selectCharacter != bindCharacter);

                    break;

                default:
                    // ��1�t���[������gameObject�̃A�N�e�B�u�؂�ւ��𕡐���s���Ɛ��������f����Ȃ����߁A���ʂ������ċC�����������ȉ��̔��蕶��p����
                    BattleUISystem.Instance.TogglePlayerParameter(selectCharacter != null && selectCharacter.param.characterTag == Character.CHARACTER_TAG.PLAYER);
                    BattleUISystem.Instance.ToggleEnemyParameter(selectCharacter != null && selectCharacter.param.characterTag == Character.CHARACTER_TAG.ENEMY);

                    // �p�����[�^�\�����X�V
                    if (selectCharacter != null)
                    {
                        if (selectCharacter.param.characterTag == Character.CHARACTER_TAG.PLAYER)
                        {
                            PlayerParameter.SetDisplayCharacter(selectCharacter);
                        }
                        else
                        {
                            EnemyParameter.SetDisplayCharacter(selectCharacter);
                        }
                    }

                    break;
            }

            // �O�t���[���őI�������L�����N�^�[�ƌ��ݑI�����Ă���L�����N�^�[���قȂ�ꍇ�̓J�������C���[�����ɖ߂�
            if (_prevCharacter != null && _prevCharacter != selectCharacter)
            {
                _prevCharacter.gameObject.SetLayerRecursively(LayerMask.NameToLayer(Constants.LAYER_NAME_CHARACTER));
            }

            // �I�����Ă���L�����N�^�[�̃��C���[���p�����[�^UI�\���̂��߂Ɉꎞ�I�ɕύX
            if (selectCharacter != null && _prevCharacter != selectCharacter)
            {
                selectCharacter.gameObject.SetLayerRecursively(LayerMask.NameToLayer(Constants.LAYER_NAME_LEFT_PARAM_WINDOW));
            }

            _prevCharacter = selectCharacter;
        }

        public void ShowDirection()
        {
            AttackDirection.gameObject.SetActive(true);
        }
    }
}