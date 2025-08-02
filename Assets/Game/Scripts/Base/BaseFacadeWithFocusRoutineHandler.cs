using Zenject;

public abstract class BaseFacadeWithFocusRoutineHandler<TH, TP>
    where TH : BaseHandlerExtendedFocusRoutine
    where TP : BasePresenter
{
    [Inject] protected TH handler;
    [Inject] protected TP presenter;

    virtual public void Init()
    {
        handler.Init();
        presenter.Init();
    }
}