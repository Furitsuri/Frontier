using Frontier;
using Frontier.Entities;
using Frontier.Stage;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StageGroup
{
    [SerializeField]
    [Header("タイル")]
    public GameObject _tileObj;

    [SerializeField]
    [Header("メッシュ")]
    public GameObject _meshObj;

    [SerializeField]
    [Header("ギミック")]
    public GameObject _gimickObj;
}

public class StageEditorHierarchyBuilder : HierarchyBuilderBase
{
    [SerializeField]
    [Header("カメラオブジェクト")]
    private GameObject _cameraObj;

    [SerializeField]
    [Header("ステージオブジェクトグループ")]
    private StageGroup _stageObjGrp;

    private void Awake()
    {
        if (_generator == null)
        {
            _generator = gameObject.AddComponent<Generator>();
        }

        Debug.Assert(
            _cameraObj != null ||
            _stageObjGrp._tileObj != null ||
            _stageObjGrp._gimickObj != null,
            "Required object reference is missing.");
    }

    override protected GameObject MapObjectToType<T>(T original)
    {
        if (original == null)
        {
            Debug.LogWarning("Null object passed as argument");

            return null;
        }

        // 名前空間の末尾部分の文字列で判定を行う
        var ns = original.GetType().Namespace;
        string[] parts = ns != null ? ns.Split('.') : Array.Empty<string>();
        return 1 <= parts.Length ? parts[parts.Length - 1] switch
        {
            "Camera" => _cameraObj,
            "Stage" => original switch
            {
                Tile => _stageObjGrp._tileObj,
                Player => _stageObjGrp._gimickObj,
                _ => this.gameObject
            },
            _ => this.gameObject
        } : this.gameObject;
    }
}