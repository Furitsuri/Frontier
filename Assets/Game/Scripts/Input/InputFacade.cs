﻿using Frontier;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using Zenject.ReflectionBaking.Mono.Cecil.Cil;
using static Constants;

public class InputFacade
{
    /// <summary>
    /// 指定のKeyCodeがtrueであれば有効,
    /// falseであれば無効を表します
    /// </summary>
    public struct ToggleInputCode
    {
        // キーアイコン
        public GuideIcon Icon;
        // アイコンに対する説明文
        public string Explanation;
        // 有効・無効を判定するコールバック
        public EnableCallback EnableCb;
        // キーを押下した際にコールバックされる関数
        public InputCallback InputCb;
        // キー処理を有効にするインターバル
        public float Interval;

        /// <summary>
        /// 入力コードを設定します
        /// </summary>
        /// <param name="enableCb">入力受付判定のコールバック</param>
        /// <param name="icon">ガイドアイコン</param>
        /// <param name="expl">説明文</param>
        /// <param name="inputCb">入力時のコールバック</param>
        public ToggleInputCode( GuideIcon icon, string expl, EnableCallback enableCb, InputCallback inputCb, float interval )
        {
            Icon            = icon;
            Explanation     = expl;
            EnableCb        = enableCb;
            InputCb         = inputCb;
            Interval        = interval;
        }

        /// <summary>
        /// オペレーター
        /// </summary>
        /// <param name="tuple">オペレーター対象の設定</param>
        public static implicit operator ToggleInputCode( ( GuideIcon, string, EnableCallback, InputCallback, float ) tuple)
        {
            return new ToggleInputCode( tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5 );
        }
    }

    private HierarchyBuilder _hierarchyBld      = null;
    private InputGuidePresenter _inputGuideView = null;
    private UISystem _uiSystem                  = null;
    private InputHandler _inputHdr              = null;
    private ToggleInputCode[] _inputCodes;

    public delegate bool EnableCallback();
    public delegate void InputCallback();

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
        _inputCodes = new ToggleInputCode[(int)Constants.GuideIcon.NUM_MAX]
        {
            ( Constants.GuideIcon.ALL_CURSOR,          "", null, null, 0.0f ),
            ( Constants.GuideIcon.VERTICAL_CURSOR,     "", null, null, 0.0f ),
            ( Constants.GuideIcon.HORIZONTAL_CURSOR,   "", null, null, 0.0f ),
            ( Constants.GuideIcon.DECISION,            "", null, null, 0.0f ),
            ( Constants.GuideIcon.CANCEL,              "", null, null, 0.0f ),
            ( Constants.GuideIcon.ESCAPE,              "", null, null, 0.0f )
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
            _inputCodes[i].Explanation  = "";
            _inputCodes[i].EnableCb     = null;
            _inputCodes[i].InputCb      = null;
            _inputCodes[i].Interval     = 0.0f;
        }
    }

    /// <summary>
    /// 現在のゲーム遷移において有効とする操作入力を画面上に表示するガイドUIと併せて登録します。
    /// また、そのキーを押下した際の処理をコールバックとして登録します
    /// </summary>
    /// <param name="args">登録するアイコン、その説明文、及び押下時に対応する処理の関数コールバック</param>
    public void RegisterInputCodes( params ToggleInputCode[] args )
    {
        foreach( var arg in args )
        {
            _inputCodes[(int)arg.Icon] = arg;
        }

        // ガイドアイコンを登録
        _inputGuideView.RegisterInputGuides();
    }

    /// <summary>
    /// 登録している入力コードを初期化した上で、
    /// 現在のゲーム遷移において有効とする操作入力を画面上に表示するガイドUIと併せて登録します。
    /// また、そのキーを押下した際の処理をコールバックとして登録します
    /// </summary>
    /// <param name="args">登録するアイコン、その説明文、及び押下時に対応する処理の関数コールバック</param>
    public void ReRegisterInputCodes( params ToggleInputCode[] args )
    {
        InitInputCodes();

        RegisterInputCodes( args );
    }

    /// <summary>
    /// EnableCallback使用時、必ず有効である場合に使用します
    /// </summary>
    /// <returns>有効</returns>
    static public bool Enable() { return true; }
}