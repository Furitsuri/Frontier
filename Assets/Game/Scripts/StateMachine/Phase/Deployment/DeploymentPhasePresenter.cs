using Froniter.StateMachine;
using Frontier.Entities;
using Frontier.UI;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Zenject;
using static Constants;

public class DeploymentPhasePresenter
{
    [Inject] IUiSystem uiSystem                 = null;

    private DeploymentUISystem _deployUiSystem = null;
    private Character[] _focusCharacters = new Character[DEPLOYMENT_SHOWABLE_CHARACTERS_NUM];
    private ReadOnlyCollection<DeploymentCandidate> _refDeploymentCandidates;

    public void Init()
    {
        _deployUiSystem = uiSystem.DeployUi;
        _deployUiSystem.Init();
    }

    public void Update()
    {
    }

    public void Exit()
    {
        ResetDeploymentCandidateEmission(); // 配置可能キャラクターのマテリアルエミッションを元に戻す
        _refDeploymentCandidates = null;    // 参照をクリアしておく

        _deployUiSystem.Exit();
    }

    /// <summary>
    /// 配置候補キャラクターリストから、指定されたインデックスを中心にキャラクターを抽出して表示させます
    /// </summary>
    /// <param name="focusCharaIndex"></param>
    public void SetFocusCharacters( int focusCharaIndex )
    {
        int arrayLength     = _focusCharacters.Length;  // 奇数前提であることに注意
        int centralIndex    = arrayLength / 2;
        int candidateCount  = _refDeploymentCandidates.Count;

        // 中央のインデックスを基準に、左右に配置するキャラクターを決定していく
        // 配列の端を超えた場合はループさせる
        for( int i = 0; i < _focusCharacters.Length; ++i )
        {
            int targetIndex     = focusCharaIndex + ( i - centralIndex );
            targetIndex         = ( targetIndex % candidateCount + candidateCount ) % candidateCount;

            _focusCharacters[i] = _refDeploymentCandidates[targetIndex].Character;
        }

        _deployUiSystem.CharacterSelectUi.AssignSelectCharacter( _focusCharacters );
    }

    public void ClearFocusCharacter()
    {
        _deployUiSystem.CharacterSelectUi.ClearSelectCharacter();
    }

    /// <summary>
    /// 配置候補キャラクターリストの参照を受け取ります
    /// </summary>
    /// <param name="refCandidateCharas"></param>
    public void AssignDeploymentCandidates( ReadOnlyCollection<DeploymentCandidate> refCandidateCharas )
    {
        _refDeploymentCandidates  = refCandidateCharas;
    }

    /// <summary>
    /// キャラクター選択UIの表示・非表示を切り替えます
    /// </summary>
    /// <param name="isActive"></param>
    public void SetActiveCharacterSelectUis( bool isActive )
    {
        _deployUiSystem.CharacterSelectUi.gameObject.SetActive( isActive );
    }

    /// <summary>
    /// 配置完了確認UIの表示・非表示を切り替えます
    /// </summary>
    /// <param name="isActive"></param>
    public void SetActiveConfirmUis( bool isActive )
    {
        _deployUiSystem.DeployMessage.SetActive( !isActive );   // 配置メッセージはConfirmUIが表示されている間は非表示にする
        _deployUiSystem.CharacterSelectUi.gameObject.SetActive( !isActive );

        _deployUiSystem.ConfirmCompleted.gameObject.SetActive( isActive );
    }

    /// <summary>
    /// 配置完了確認UIのテキストカラーの選択・非選択状態を適用します
    /// </summary>
    /// <param name="selectIndex"></param>
    public void ApplyTextColor2ConfirmCompleted( int selectIndex )
    {
        _deployUiSystem.ConfirmCompleted.ApplyTextColor( selectIndex );
    }

    public void RefreshDeploymentCandidateEmission()
    {
        foreach( var candidate in _refDeploymentCandidates )
        {
            candidate.Character.SetMaterialEmission( !candidate.IsDeployed );
        }
    }

    private void ResetDeploymentCandidateEmission()
    {
        foreach( var candidate in _refDeploymentCandidates )
        {
            candidate.Character.SetMaterialEmission( true );
        }
    }   
}
