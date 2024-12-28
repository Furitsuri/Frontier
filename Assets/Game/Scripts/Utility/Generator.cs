using Frontier;
using System.Collections;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using UnityEngine;
using Zenject;

public class Generator : MonoBehaviour
{
    private DiContainer _container;
    private DiInstaller _installer;

    /// <summary>
    /// DI�R���e�i�̃C���X�^���X�ɒ������܂�
    /// </summary>
    /// <param name="container">DI�R���e�i</param>
    public void Inject( DiContainer container, DiInstaller installer )
    {
        _container = container;
        _installer = installer;
    }

    /// <summary>
    /// �I�u�W�F�N�g�𐶐����A���̃I�u�W�F�N�g�ɃR���|�[�l���g��ǉ����܂�
    /// </summary>
    /// <typeparam name="T">�I�u�W�F�N�g�ɒǉ�����R���|�[�l���g</typeparam>
    /// <param name="initActive">�Q�[���I�u�W�F�N�g�̏����̗L���E�������</param>
    /// <returns>���������R���|�[�l���g</returns>
    public T GenerateObjectAndAddComponent<T>(bool initActive) where T : Behaviour
    {
        GameObject gameObj = new GameObject();

        T original = gameObj.AddComponent<T>();

        gameObj.SetActive(initActive);

        return original;
    }

    /// <summary>
    /// �n���ꂽ�Q�[���I�u�W�F�N�g����R���|�[�l���g�𐶐����܂�
    /// </summary>
    /// <typeparam name="T">��������Behavior���p�����ꂽ�C�ӂ̌^</typeparam>
    /// <param name="gameObject">�n���Q�[���I�u�W�F�N�g</param>
    /// <param name="initActive">�Q�[���I�u�W�F�N�g�̏����̗L���E�������</param>
    /// <returns>���������R���|�[�l���g</returns>
    public T GenerateComponentFromObject<T> ( GameObject gameObject, bool initActive ) where T : Behaviour
    {
        GameObject gameObj = Instantiate( gameObject );
        Debug.Assert(gameObj != null );

        T original = gameObj.GetComponent<T>();
        Debug.Assert( original != null );
        
        gameObj.SetActive(initActive);

        return original;
    }

    /// <summary>
    /// DI�R���e�i��p���Ďw��̃Q�[���I�u�W�F�N�g�ƃR���|�[�l���g���쐬���܂�
    /// </summary>
    /// <typeparam name="T">�쐬����R���|�[�l���g�̌^</typeparam>
    /// <param name="gameObject">�쐬����I�u�W�F�N�g</param>
    /// <param name="initActive">�Q�[���I�u�W�F�N�g�̏����̗L���E�������</param>
    /// <returns>�쐬�����R���|�[�l���g</returns>
    public T InstantiateComponentWithDIContainer<T>(GameObject gameObject, bool initActive, bool isBind) where T : Behaviour
    {
        T original = _container.InstantiatePrefabForComponent<T>(gameObject);
        Debug.Assert(original != null);

        original.gameObject.SetActive(initActive);

        // Di�R���e�i�Ƀo�C���h����ꍇ�͂����Ńo�C���h
        if (isBind)
        {
            _installer.InstallBindings(original);
        }

        return original;
    }
}