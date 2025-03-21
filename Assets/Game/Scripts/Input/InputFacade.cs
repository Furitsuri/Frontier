﻿using Frontier;
using System;
using UnityEngine;
using Zenject;
using static Constants;

public class InputFacade
{
    private HierarchyBuilder _hierarchyBld      = null;
    private InputGuidePresenter _inputGuideView = null;
    private UISystem _uiSystem                  = null;
    private InputHandler _inputHdr              = null;
    private InputCode[] _inputCodes;

    [Inject]
    public void Construct( HierarchyBuilder hierarchyBld, UISystem uiSystem )
    {
        _hierarchyBld   = hierarchyBld;
        _uiSystem       = uiSystem;
    }

    /// <summary>
    /// 判定対象となる入力コードを初期化します
    /// </summary>
    private void InitInputCodes()
    {
        _inputCodes = new InputCode[(int)Constants.GuideIcon.NUM_MAX]
        {
            ( Constants.GuideIcon.ALL_CURSOR,          "", null, null, 0.0f ),
            ( Constants.GuideIcon.VERTICAL_CURSOR,     "", null, null, 0.0f ),
            ( Constants.GuideIcon.HORIZONTAL_CURSOR,   "", null, null, 0.0f ),
            ( Constants.GuideIcon.CONFIRM,             "", null, null, 0.0f ),
            ( Constants.GuideIcon.CANCEL,              "", null, null, 0.0f ),
            ( Constants.GuideIcon.ESCAPE,              "", null, null, 0.0f ),
            ( Constants.GuideIcon.SUB1,                "", null, null, 0.0f ),
            ( Constants.GuideIcon.SUB2,                "", null, null, 0.0f ),
            ( Constants.GuideIcon.SUB3,                "", null, null, 0.0f ),
            ( Constants.GuideIcon.SUB4,                "", null, null, 0.0f )
        };
    }

    /// <summary>
    /// 初期化します
    /// </summary>
    public void Init()
    {
        // 入力コード情報を全て初期化
        InitInputCodes();

        if (_inputHdr == null)
        {
            _inputHdr = _hierarchyBld.CreateComponentAndOrganize<InputHandler>(true);
            DebugUtils.NULL_ASSERT( _inputHdr );
        }

        if( _inputGuideView == null )
        {
            _inputGuideView = _uiSystem.GeneralUi.InputGuideView;
            DebugUtils.NULL_ASSERT(_inputGuideView);
        }

        // 入力コード情報を受け渡す
        _inputHdr.Init(_inputGuideView, _inputCodes);
        _inputGuideView.Init(_inputCodes);    
    }

    /// <summary>
    /// 判定対象となる入力コードを初期化します
    /// Iconは変更しないため、そのままにします
    /// </summary>
    public void ResetInputCodes()
    {
        for ( int i = 0; i < (int)Constants.GuideIcon.NUM_MAX; ++i)
        {
            _inputCodes[i].Explanation      = "";
            _inputCodes[i].EnableCb         = null;
            _inputCodes[i].InputInterval    = 0.0f;
        }
    }

    /// <summary>
    /// 登録済みの入力コードはそのままに、
    /// 現在のゲーム遷移において有効とする操作入力を画面上に表示するガイドUIと併せて登録します。
    /// また、そのキーを押下した際の処理をコールバックとして登録します。
    /// </summary>
    /// <param name="args">登録するアイコン、その説明文、及び押下時に対応する処理の関数コールバック</param>
    public void RegisterInputCodes( params InputCode[] args )
    {
        foreach( var arg in args )
        {
            // _inputCodesが未登録であれば登録する
            if (_inputCodes[(int)arg.Icon].IsUnRegistererd())
            {
                _inputCodes[(int)arg.Icon] = arg;
            }
        }

        // ガイドアイコンを登録
        _inputGuideView.RegisterInputGuides();
    }

    /// <summary>
    /// 登録している入力コードを初期化した上で、
    /// 現在のゲーム遷移において有効とする操作入力を画面上に表示するガイドUIと併せて登録します。
    /// また、そのキーを押下した際の処理をコールバックとして登録します。
    /// </summary>
    /// <param name="args">登録するアイコン、その説明文、及び押下時に対応する処理の関数コールバック</param>
    public void ReRegisterInputCodes<T>(InputCode[] args)
    {
        ResetInputCodes();

        RegisterInputCodes( args );
    }
}