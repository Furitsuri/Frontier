using Frontier.Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Frontier.BattleFileLoader;

public class CharacterParameters
{
    private CharacterParameter _characterParam;
    private TemporaryParameter _tmpParam;
    private ModifiedParameter _modifiedParam;
    private SkillModifiedParameter _skillModifiedParam;
    private CameraParameter _camParam;

    public ref CharacterParameter CharacterParam => ref _characterParam;
    public ref TemporaryParameter TmpParam => ref _tmpParam;
    public ref ModifiedParameter ModifiedParam => ref _modifiedParam;
    public ref SkillModifiedParameter SkillModifiedParam => ref _skillModifiedParam;
    public ref CameraParameter CameraParam => ref _camParam;

    public void Awake()
    {
        _characterParam.Awake();
        _tmpParam.Awake();
    }

    public void Init()
    {
        _characterParam.Init();
        _tmpParam.Init();
        _modifiedParam.Init();
        _skillModifiedParam.Init();
        _camParam.Init();
    }
}
