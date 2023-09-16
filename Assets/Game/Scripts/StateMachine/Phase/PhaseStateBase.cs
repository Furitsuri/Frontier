using UnityEngine;

public class PhaseStateBase : TreeNode<PhaseStateBase>
{
    private bool _isBack = false;
    public int TransitIndex { get; protected set; } = -1;
    protected BattleManager _btlMgr;

    // ‰Šú‰»
    virtual public void Init()
    {
        _btlMgr = ManagerProvider.Instance.GetService<BattleManager>();
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

    protected void NoticeCharacterDied( CharacterHashtable.Key characterKey )
    {
        _btlMgr.SetDiedCharacterKey(characterKey);
    }
}
