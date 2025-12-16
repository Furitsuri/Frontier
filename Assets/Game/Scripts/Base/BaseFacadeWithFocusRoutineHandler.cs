using Zenject;

public abstract class BaseFacadeWithFocusRoutineHandler<TH, TP>
    where TH : BaseHandlerExtendedFocusRoutine
    where TP : BasePresenter
{
    [Inject] protected TH _handler;
    [Inject] protected TP _presenter;

    virtual public void Init()
    {
        _handler.Init();
        _presenter.Init();
    }
}