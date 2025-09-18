using Frontier.Stage;
namespace Frontier.Stage
{
    /// <summary>
    /// ステージデータ提供インターフェース
    /// ステージデータを直接取得出来るようにすると、
    /// C#の仕様上、ステージを編集・読込する際に新しいデータへと差し替えることが出来ないため
    /// 間接的に取得するインターフェースが必要
    /// </summary>
    public interface IStageDataProvider
    {
        StageData CurrentData { get; set; }
    }
}