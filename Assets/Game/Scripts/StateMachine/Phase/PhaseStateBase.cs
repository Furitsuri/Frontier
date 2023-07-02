using UnityEngine;

public class PhaseStateBase : TreeNode<PhaseStateBase>
{
    private bool _isBack = false;
    public int TransitIndex { get; protected set; } = -1;

    // ‰Šú‰»
    virtual public void Init()
    {
        TransitIndex = -1;
        _isBack = false;
    }

    // XV
    virtual public bool Update()
    {
        if( Input.GetKeyUp( KeyCode.Backspace ) )
        {
            Back();

            return true;
        }

        return false;
    }

    // ‘Ş”ğ
    virtual public void Exit()
    {
    }

    // –ß‚é
    virtual public bool IsBack()
    {
        return _isBack;
    }

    /// <summary>
    /// e‚Ì‘JˆÚ‚É–ß‚è‚Ü‚·
    /// </summary>
    protected void Back()
    {
        _isBack = true;
    }

    protected void NoticeCharacterDied( Character.CHARACTER_TAG characterTag )
    {
        BattleManager.Instance.SetDiedCharacterTag(characterTag);
    }
}
