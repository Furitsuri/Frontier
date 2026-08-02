using UnityEngine;

public class GetVectorInput : IGetInputBase, IGetGenericInput<Vector2>
{
    public delegate Vector2 GetVectorInputCallback();

    private GetVectorInputCallback _callback;

    public GetVectorInput( GetVectorInputCallback callback )
    {
        _callback = callback;
    }

    public Vector2 GetGenericInput()
    {
        return _callback();
    }

    object IGetInputBase.GetInput()
    {
        return GetGenericInput();
    }

    public void Apply( InputContext context, InputTriggerMode mode )
    {
        context.Stick = _callback();
    }

    /// <summary>
    /// ベクトル系入力(マウス移動量等)には「押されている/いない」の概念が無いため、常にtrueを返します
    /// (RepeatDelayによる制御の対象外として扱われます)。
    /// </summary>
    public bool IsHeld( InputContext context )
    {
        return true;
    }
}