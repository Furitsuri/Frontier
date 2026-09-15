using Frontier.StateMachine;

public interface IConfirmPresenter
{
    /// <summary>
    /// 確認UIの表示/非表示を切り替えます。uiTypeは選択操作の方式(ConfirmUIType参照)であり、
    /// 会話ウィンドウ形式(TalkWindowConfirmPresenter)以外の実装ではSUB1/SUB2アイコン表示に
    /// 対応しないため無視して構いません。
    /// </summary>
    public void SetActiveConfirmUI( bool isActive, ConfirmUIType uiType );

    public void ApplyColor2Options( int selectIndex );
}