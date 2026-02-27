using Frontier.Battle;
using Frontier.Entities;
using Frontier.UI;
using UnityEngine;
using Zenject;
using static Constants;

public class DeploymentPhasePresenter : CharacterSelectionPresenter, IConfirmPresenter
{
    [Inject] protected BattleRoutineController _btlRtnCtrl = null;

    private Character _currentGridSelectCharacter   = null;
    private DeploymentUISystem _deployUiSystem      = null;

    public void Init()
    {
        base.Init( _uiSystem.DeployUi.CharacterSelectUI, DEPLOYMENT_SHOWABLE_CHARACTERS_NUM );

        _deployUiSystem = _uiSystem.DeployUi;
        _deployUiSystem.Init();
    }

    public void Update()
    {
        UpdateSlideAnimation();
    }

    public override void Exit()
    {
        base.Exit();

        _deployUiSystem.Exit();
    }

    public void SetActiveConfirmUI( bool isActive )
    {
        _deployUiSystem.CharacterSelectUI.SetActive( !isActive );               // 配置キャラクター選択UIの表示を切替
        _deployUiSystem.ConfirmCompletedUI.gameObject.SetActive( isActive );    // 配置完了確認UIの表示を切替
    }

    public void SetRemainingDeployableNum( int num )
    {
        _deployUiSystem.RemainingDeployments.RemainingNum.text = num.ToString();
        if( num <= 0 )
        {
            _deployUiSystem.RemainingDeployments.RemainingNum.color = Color.red;
        }
        else
        {
            _deployUiSystem.RemainingDeployments.RemainingNum.color = Color.yellow;
        }
    }

    public void ApplyColor2Options( int selectIndex )
    {
        _deployUiSystem.ConfirmCompletedUI.ApplyTextColor( selectIndex );
    }

    /// <summary>
    /// グリッドカーソルが選択中のキャラクター情報を更新します
    /// </summary>
    public void RefreshGridCursorSelectCharacter()
    {
        // グリッドカーソルが現在選択中のキャラクターを取得
        _currentGridSelectCharacter = _btlRtnCtrl.BtlCharaCdr.GetSelectCharacter();

        // タイル上のキャラクターのパラメータは、フォーカス中の配置候補キャラクター以外であれば表示
        bool isActiveOnSelectCharaParam = ( null != _currentGridSelectCharacter && _currentGridSelectCharacter != _focusCandidates[_focusCandidates.Length / 2].Character );
        _deployUiSystem.GridCursorSelectCharaParam.gameObject.SetActive( isActiveOnSelectCharaParam );
        if( !isActiveOnSelectCharaParam ) { return; }

        if( null != _currentGridSelectCharacter )
        {
            _deployUiSystem.GridCursorSelectCharaParam.AssignCharacter( _currentGridSelectCharacter, LAYER_MASK_INDEX_DEPLOYMENT_GRID );
        }

        // 配置候補UI内でフォーカス中のキャラクターも更新
        // MEMO : RefreshFocusDeploymentCharacter()を呼んでしまうと無限ループに陥るため注意
        // _deployUiSystem.CharacterSelectUI.FocusCharaParamUI.AssignCharacter( _focusCandidates[_focusCandidates.Length / 2].Character, LAYER_MASK_INDEX_DEPLOYMENT_FOCUS );
    }

    /// <summary>
    /// フォーカス中の配置可能キャラクター情報を更新します
    /// </summary>
    public void RefreshFocusDeploymentCharacter()
    {
        // フォーカス中のキャラクターのパラメータの表示
        Debug.Assert( _focusCandidates.Length % 2 == 1 );  // 奇数であることが前提
        _deployUiSystem.CharacterSelectUI.FocusCharaParamUI.AssignCharacter( _focusCandidates[_focusCandidates.Length / 2].Character, LAYER_MASK_INDEX_DEPLOYMENT_FOCUS );

        RefreshGridCursorSelectCharacter();
    }

    public ( int, int ) GetCharacterSelectionDisplaySize()
    {
        var size = _deployUiSystem.CharacterSelectUI.GetCharacterSelectionDisplaySize();

        return ( (int)size.Item1, ( int ) size.Item2 );
    }
}