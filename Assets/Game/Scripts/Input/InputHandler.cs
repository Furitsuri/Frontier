using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class InputHandler : MonoBehaviour
{
    // 入力ガイド表示
    private InputGuidePresenter _inputGuideView = null;
    // 前フレームで入力が有効であったかの確認
    private bool[] _prevEnableCbs               = null;
    // 最後に入力操作をした時間の保持
    private float _operateInputLastTime         = 0.0f;
    // InputFacade内のInputCodeの参照値(書き換え不可)
    private ReadOnlyCollection<InputFacade.ToggleInputCode> _refInputCodes;

    // Start is called before the first frame update
    void Start()
    {
        _prevEnableCbs = new bool[(int)Constants.GuideIcon.NUM_MAX];
    }

    // Update is called once per frame
    void Update()
    {
        UpdateInputCodes();
    }

    /// <summary>
    /// 入力情報の判定・走査を更新します
    /// </summary>
    private void UpdateInputCodes()
    {
        // コールバック関数が設定されている場合は動作させる
        foreach (var code in _refInputCodes)
        {
            int codeIdx = (int)code.Icon;
            bool enable = code.EnableCb != null && code.EnableCb();

            if (enable)
            {
                if (code.InputCb != null)
                {
                    code.InputCb();
                }
            }
        }
    }

    /// <summary>
    /// ユーザーがキー操作を行った際に、
    /// 短い時間で何度も同じキーが押下されたと判定されないためにインターバル時間を設けます
    /// </summary>
    /// <returns>キー操作が有効か無効か</returns>
    private bool OperateInputControl()
    {
        if (Constants.OPERATE_KET_INTERVAL <= Time.time - _operateInputLastTime)
        {
            _operateInputLastTime = Time.time;

            return true;
        }

        return false;
    }

    /// <summary>
    /// 初期化します
    /// </summary>
    /// <param name="inputGuidePresenter">入力ガイド表示クラス</param>
    /// <param name="inputCodes">入力情報コード</param>
    public void Init( InputGuidePresenter inputGuidePresenter, InputFacade.ToggleInputCode[] inputCodes )
    {
        _inputGuideView    = inputGuidePresenter;
        _refInputCodes     = Array.AsReadOnly( inputCodes );
    }
}