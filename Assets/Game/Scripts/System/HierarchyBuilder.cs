using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier
{
    [System.Serializable]
    public class CharacterGroup
    {
        [SerializeField]
        [Header("�v���C���[")]
        public GameObject _playerObj;

        [SerializeField]
        [Header("�G�l�~�[")]
        public GameObject _enemyObj;
    }

    /// <summary>
    /// �I�u�W�F�N�g�E�R���|�[�l���g�쐬�N���X
    /// </summary>
    public class HierarchyBuilder : MonoBehaviour
    {
        [SerializeField]
        [Header("�J�����I�u�W�F�N�g")]
        private GameObject _cameraObj;

        [SerializeField]
        [Header("�L�����N�^�[�I�u�W�F�N�g")]
        private CharacterGroup _characterObjGrp;

        [SerializeField]
        [Header("�R���g���[���I�u�W�F�N�g")]
        private GameObject _controllerObj;

        [SerializeField]
        [Header("�}�l�[�W���[�I�u�W�F�N�g")]
        private GameObject _managerObj;

        // �I�u�W�F�N�g�����N���X
        Generator _generator = null;

        /// <summary>
        /// DiInstaller����Ăяo���A�R���e�i��o�^���܂�
        /// </summary>
        /// <param name="container">DI�R���e�i</param>
        [Inject]
        void Construct(DiContainer container, DiInstaller installer)
        {
            if (_generator == null)
            {
                _generator = gameObject.AddComponent<Generator>();
            }

            _generator.Inject(container, installer);
        }

        void Awake()
        {
            if (_generator == null)
            {
                _generator = gameObject.AddComponent<Generator>();
            }

            Debug.Assert(
                _cameraObj != null ||
                _characterObjGrp._playerObj != null ||
                _characterObjGrp._enemyObj != null ||
                _controllerObj != null ||
                _managerObj != null,
                "Required object reference is missing.");
        }

        /// <summary>
        /// �����Ɏw�肵���r�w�C�r�A�̕R�Â���̃I�u�W�F�N�g�����肵�܂�
        /// </summary>
        /// <param name="original">�R�Â����s���ΏۃI�u�W�F�N�g</param>
        /// <returns>�R�Â���̃I�u�W�F�N�g</returns>
        private GameObject MapObjectToType<T>( T original )
        {
            if (original == null)
            {
                Debug.LogWarning("Null object passed as argument");

                return null;
            }
            
            return original switch
            {
                Camera => _cameraObj,
                Player => _characterObjGrp._playerObj,
                Enemy => _characterObjGrp._enemyObj,
                Controller => _controllerObj,
                Manager => _managerObj,
                _ => this.gameObject
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private object HandlePlayer()
        {
            // Player�^�ɑ΂��鏈��
            Debug.Log("Handling Player type");
            return "Player-specific value";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="enemy"></param>
        /// <returns></returns>
        private object HandleEnemy(Enemy enemy)
        {
            // Enemy�^�ɑ΂��鏈��
            Debug.Log("Handling Enemy type");
            return "Enemy-specific value";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private object HandleDefault()
        {
            // ���̑���GameObject�^�ɑ΂��鏈��
            Debug.Log("Handling default GameObject type");
            return "Default value";
        }

        /// <summary>
        /// �������ꂽ�I�u�W�F�N�g���w��̃I�u�W�F�N�g�̊K�w���ɔz�u���܂�
        /// </summary>
        /// <param name="bhv">�������ꂽ�I�u�W�F�N�g</param>
        private void Organize(Behaviour bhv)
        {
            GameObject parentObj = MapObjectToType(bhv);

            if (parentObj != null)
            {
                bhv.transform.SetParent( parentObj.transform );
            }
        }

        /// <summary>
        /// �I�u�W�F�N�g�y�уR���|�[�l���g���쐬���A�q�G�����L�[��̔C�ӂ̃I�u�W�F�N�g�̊K�w���ɐݒu���܂�
        /// </summary>
        /// <typeparam name="T">�쐬����R���|�[�l���g�̌^</typeparam>
        /// <param name="initActive">�쐬�����I�u�W�F�N�g�̏����̗L���E�������</param>
        /// <returns>�쐬�����R���|�[�l���g</returns>
        public T CreateComponentAndOrganize<T>( bool initActive) where T : Behaviour
        {
            T generateCpt = _generator.GenerateObjectAndAddComponent<T>(initActive);
            Debug.Assert(generateCpt != null);

            Organize(generateCpt);

            return generateCpt;
        }

        /// <summary>
        /// �����ɓn�����I�u�W�F�N�g����R���|�[�l���g���쐬���A�q�G�����L�[��̔C�ӂ̃I�u�W�F�N�g�̊K�w���ɐݒu���܂�
        /// </summary>
        /// <typeparam name="T">�쐬����R���|�[�l���g�̌^</typeparam>
        /// <param name="gameObject">�R���|�[�l���g�̌��ƂȂ�Q�[���I�u�W�F�N�g</param>
        /// <param name="initActive">�Q�[���I�u�W�F�N�g�̏����̗L���E�������</param>
        /// <returns>�쐬�����R���|�[�l���g</returns>
        public T CreateComponentAndOrganize<T>(GameObject gameObject, bool initActive) where T : Behaviour
        {
            T generateCpt = _generator.GenerateComponentFromObject<T>(gameObject, initActive);
            Debug.Assert(generateCpt != null);

            Organize(generateCpt);

            return generateCpt;
        }

        /// <summary>
        /// �����ɓn�����I�u�W�F�N�g����R���|�[�l���g���쐬���܂�
        /// �܂��A�쐬�����R���|�[�l���g��e�Ƃ���I�u�W�F�N�g�̎q�Ƃ��Đݒ肵�A�q�G�����L�[��̔C�ӂ̃I�u�W�F�N�g�̊K�w���ɐݒu���܂�
        /// </summary>
        /// <typeparam name="T">�쐬����R���|�[�l���g�̌^</typeparam>
        /// <param name="gameObject">�R���|�[�l���g�̌��ƂȂ�Q�[���I�u�W�F�N�g</param>
        /// <param name="parentObject">�q�G�����L�[��ō쐬�����I�u�W�F�N�g�̐e�ƂȂ�I�u�W�F�N�g</param>
        /// <param name="initActive">�Q�[���I�u�W�F�N�g�̏����̗L���E�������</param>
        /// <returns>�쐬�����R���|�[�l���g</returns>
        public T CreateComponentWithNestedParent<T>(GameObject gameObject, GameObject parentObject, bool initActive) where T : Behaviour
        {
            T generateCpt = _generator.GenerateComponentFromObject<T>(gameObject, initActive);
            Debug.Assert(generateCpt != null);

            generateCpt.transform.parent = parentObject.transform;

            return generateCpt;
        }

        /// <summary>
        /// �����ɓn�����I�u�W�F�N�g����R���|�[�l���g���쐬���܂�
        /// �܂��A�쐬�����R���|�[�l���g��e�Ƃ���I�u�W�F�N�g�̎q�Ƃ��Đݒ肵�A�X�Ɏw��̖��O�ō쐬�����f�B���N�g���̎q�Ƃ��Ă��̐e��ݒ�̏�A
        /// �q�G�����L�[��̔C�ӂ̃I�u�W�F�N�g�̊K�w���ɐݒu���܂�
        /// </summary>
        /// <typeparam name="T">�쐬����R���|�[�l���g�̌^</typeparam>
        /// <param name="gameObject">�R���|�[�l���g�̌��ƂȂ�Q�[���I�u�W�F�N�g</param>
        /// <param name="parentObject">�q�G�����L�[��ō쐬�����I�u�W�F�N�g�̐e�ƂȂ�I�u�W�F�N�g</param>
        /// <param name="newDirectoryObjectName">�쐬����q�G�����L�[��̃I�u�W�F�N�g(�f�B���N�g���̑�ւƂȂ��I�u�W�F�N�g)�̖��O</param>
        /// <param name="initActive">�Q�[���I�u�W�F�N�g�̏����̗L���E�������</param>
        /// <returns>�쐬�����R���|�[�l���g</returns>
        public T CreateComponentWithNestedNewDirectory<T>(GameObject gameObject, GameObject parentObject, string newDirectoryObjectName, bool initActive) where T : Behaviour
        {
            T generateCpt = _generator.GenerateComponentFromObject<T>(gameObject, initActive);
            Debug.Assert(generateCpt != null);

            GameObject folderObject = new GameObject(newDirectoryObjectName);
            folderObject.transform.parent = parentObject.transform;
            generateCpt.transform.parent = folderObject.transform;

            return generateCpt;
        }

        /// <summary>
        /// DI�R���e�i��p���ăI�u�W�F�N�g�y�уR���|�[�l���g���쐬���A�q�G�����L�[��̔C�ӂ̃I�u�W�F�N�g�̊K�w���ɐݒu���܂�
        /// </summary>
        /// <typeparam name="T">�쐬����R���|�[�l���g�̌^</typeparam>
        /// <param name="initActive">�Q�[���I�u�W�F�N�g�̏����̗L���E�������</param>
        /// <returns>�쐬�����R���|�[�l���g</returns>
        public T CreateComponentAndOrganizeWithDiContainer<T>( bool initActive, bool isBind ) where T : Behaviour
        {
            GameObject gameObj = new GameObject();
            T generateCpt = _generator.InstantiateComponentWithDIContainer<T>(gameObject, initActive, isBind);

            return generateCpt;
        }

        /// <summary>
        /// �����ɓn�����I�u�W�F�N�g����DI�R���e�i��p���ăR���|�[�l���g���쐬���A�q�G�����L�[��̔C�ӂ̃I�u�W�F�N�g�̊K�w���ɐݒu���܂�
        /// </summary>
        /// <typeparam name="T">�쐬����R���|�[�l���g�̌^</typeparam>
        /// <param name="gameObject">�R���|�[�l���g�̌��ƂȂ�Q�[���I�u�W�F�N�g</param>
        /// <param name="initActive">�Q�[���I�u�W�F�N�g�̏����̗L���E�������</param>
        /// <returns>�쐬�����R���|�[�l���g</returns>
        public T CreateComponentAndOrganizeWithDiContainer<T>(GameObject gameObject, bool initActive, bool isBind) where T : Behaviour
        {
            T generateCpt = _generator.InstantiateComponentWithDIContainer<T>(gameObject, initActive, isBind);

            return generateCpt;
        }

        /// <summary>
        /// �����ɓn�����I�u�W�F�N�g����DI�R���e�i��p���ăR���|�[�l���g���쐬���܂�
        /// �܂��A�쐬�����R���|�[�l���g��e�Ƃ���I�u�W�F�N�g�̎q�Ƃ��Đݒ肵�A�q�G�����L�[��̔C�ӂ̃I�u�W�F�N�g�̊K�w���ɐݒu���܂�
        /// </summary>
        /// <typeparam name="T">�쐬����R���|�[�l���g�̌^</typeparam>
        /// <param name="gameObject">�R���|�[�l���g�̌��ƂȂ�Q�[���I�u�W�F�N�g</param>
        /// <param name="parentObject">�q�G�����L�[��ō쐬�����I�u�W�F�N�g�̐e�ƂȂ�I�u�W�F�N�g</param>
        /// <param name="initActive">�Q�[���I�u�W�F�N�g�̏����̗L���E�������</param>
        /// <returns>�쐬�����R���|�[�l���g</returns>
        public T CreateComponentWithNestedParentWithDiContainer<T>(GameObject gameObject, GameObject parentObject, bool initActive, bool isBind) where T : Behaviour
        {
            T generateCpt = _generator.InstantiateComponentWithDIContainer<T>(gameObject, initActive, isBind);

            generateCpt.transform.parent = parentObject.transform;

            return generateCpt;
        }

        /// <summary>
        /// �����ɓn�����I�u�W�F�N�g����DI�R���e�i��p���ăR���|�[�l���g���쐬���܂�
        /// �܂��A�쐬�����R���|�[�l���g��e�Ƃ���I�u�W�F�N�g�̎q�Ƃ��Đݒ肵�A�X�Ɏw��̖��O�ō쐬�����f�B���N�g���̎q�Ƃ��Ă��̐e��ݒ�̏�A
        /// �q�G�����L�[��̔C�ӂ̃I�u�W�F�N�g�̊K�w���ɐݒu���܂�
        /// </summary>
        /// <typeparam name="T">�쐬����R���|�[�l���g�̌^</typeparam>
        /// <param name="gameObject">�R���|�[�l���g�̌��ƂȂ�Q�[���I�u�W�F�N�g</param>
        /// <param name="parentObject">�q�G�����L�[��ō쐬�����I�u�W�F�N�g�̐e�ƂȂ�I�u�W�F�N�g</param>
        /// <param name="newDirectoryObjectName">�쐬����q�G�����L�[��̃I�u�W�F�N�g(�f�B���N�g���̑�ւƂȂ��I�u�W�F�N�g)�̖��O</param>
        /// <param name="initActive">�Q�[���I�u�W�F�N�g�̏����̗L���E�������</param>
        /// <returns>�쐬�����R���|�[�l���g</returns>
        public T CreateComponentWithNestedNewDirectoryWithDiContainer<T>(GameObject gameObject, GameObject parentObject, string newDirectoryObjectName, bool initActive, bool isBind) where T : Behaviour
        {
            T generateCpt = _generator.InstantiateComponentWithDIContainer<T>(gameObject, initActive, isBind);

            GameObject folderObject = new GameObject(newDirectoryObjectName);
            folderObject.transform.parent = parentObject.transform;
            generateCpt.transform.parent = folderObject.transform;

            return generateCpt;
        }
    }
}