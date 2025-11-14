using Frontier.Stage;

public class DeployableTileData
{
    private bool _isDeployable;
    private Tile _tile;

    public bool IsDeployable => _isDeployable;
    public Tile Tile => _tile;

    public void Init( bool isDeployable, Tile tile  )
    {
        _isDeployable   = isDeployable;
        _tile           = tile;

        if( !isDeployable )
        {
            _tile.SetUndeployableColor();   // 配置不可能なことを示すためにタイルの色を変更する
        }
    }

    public void Dispose()
    {
        _tile.ClearUndeployableColor(); // タイルの色を元に戻す
    }
}