using Zenject;

public abstract class BaseFacade<TH, TP>
    where TH : BaseHandler
    where TP : BasePresenter
{
    [Inject] protected TH handler;
    [Inject] protected TP presenter;

    public virtual void Initialize()
    {
        handler.Init();
        presenter.Init();
    }
}