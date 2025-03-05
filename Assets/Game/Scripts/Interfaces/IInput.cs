
public interface IInput
{
    public Constants.Direction GetDirectionalPressed();

    public bool IsConfirmPressed();

    public bool IsCancelPressed();

    public bool IsPausePressed();

    public bool IsOptionsPressed();
}