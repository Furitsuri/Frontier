using Frontier;
using Frontier.UI;

public interface IUiSystem
{
    public GeneralUISystem GeneralUi { get; }
    public DeploymentUISystem DeployUi { get; }
    public BattleUISystem BattleUi { get; }
#if UNITY_EDITOR
    public DebugUISystem DebugUi { get; }
#endif // UNITY_EDITOR
    public void InitializeUiSystem();
}