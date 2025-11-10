using Frontier.Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using static Constants;
using static Frontier.CharacterParameterUI;

public class DeploymentCharacterSelectUI : MonoBehaviour
{
    [Inject] private HierarchyBuilderBase _hierarchyBld = null;

    [SerializeField] private RawImage[] TargetImages = new RawImage[DEPLOYMENT_SHOWABLE_CHARACTERS_NUM];

    private CharacterCamera[] _charaCameras = new CharacterCamera[DEPLOYMENT_SHOWABLE_CHARACTERS_NUM];

    private void Awake()
    {
        for( int i = 0; i < DEPLOYMENT_SHOWABLE_CHARACTERS_NUM; ++i )
        {
            _charaCameras[i] = _hierarchyBld.InstantiateWithDiContainer<CharacterCamera>( false );
            NullCheck.AssertNotNull( _charaCameras[i], "_charaCamera" );
        }

        gameObject.SetActive( false );
    }

    void Start()
    {
        for( int i = 0; i < DEPLOYMENT_SHOWABLE_CHARACTERS_NUM; ++i )
        {
            _charaCameras[i].Init( "CharaParamCamera_Deploy", LAYER_NAME_DEPLOY, ref TargetImages[i] );
        }
    }

    private void Update()
    {
        CameraParameter camParam = new CameraParameter(
            new Vector3( 0.0f, 1.5f, 0.5f ),
            1.0f,
            1.5f,
            1.0f
        );

        for( int i = 0; i < DEPLOYMENT_SHOWABLE_CHARACTERS_NUM; ++i )
        {
            _charaCameras[i].Update( in camParam );
        }
    }

    public void AssignSelectCharacter( Character[] selectCharacters )
    {
        for( int i = 0; i < DEPLOYMENT_SHOWABLE_CHARACTERS_NUM; ++i )
        {
            _charaCameras[i].SetDisplayCharacter( selectCharacters[i], LAYER_NAME_DEPLOY );
        }
    }

    public void ClearSelectCharacter()
    {
        for( int i = 0; i < DEPLOYMENT_SHOWABLE_CHARACTERS_NUM; ++i )
        {
            _charaCameras[i].ClearDisplayCharacter();
        }
    }
}
