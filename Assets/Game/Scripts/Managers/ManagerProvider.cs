using UnityEngine;

public class ManagerProvider : Singleton<ManagerProvider>, IServiceProvider
{
    [SerializeField]
    private GameObject _battleManagerObject;
    [SerializeField]
    private GameObject _soundManager;

    private BattleManager _battleManager;

    override protected void Init()
    {
        GameObject btlMgr = Instantiate( _battleManagerObject );
        if( btlMgr != null )
        {
            _battleManager = btlMgr.GetComponent<BattleManager>();
        }

        if( _soundManager == null )
        {
            Instantiate( _soundManager );
        }
    }

    public T GetService<T>()
    {
        if( typeof(T) == typeof(BattleManager) )
        {
            return (T)(object)_battleManager;
        }
        if (typeof(T) == typeof(SoundManager))
        {
            return (T)(object)_soundManager;
        }

        return default(T);
    }
}
