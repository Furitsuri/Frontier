using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class TutorialFileLoader : MonoBehaviour
{
    [SerializeField]
    [Header("チュートリアルデータ(ScriptableObject)のファイル名")]
    private string _tutorialDatabaseFileName;

    private TutorialDatabase _tutorialDatabase;

    /// <summary>
    /// チュートリアルデータを読込みます
    /// </summary>
    public void LoadData()
    {
        Addressables.LoadAssetAsync<TutorialDatabase>(_tutorialDatabaseFileName).Completed += OnLoaded;
    }

    /// <summary>
    /// 読み込んだチュートリアルデータを取得します
    /// </summary>
    /// <returns>チュートリアルデータ</returns>
    public TutorialData[] GetTutorialDatas()
    {
        return _tutorialDatabase.GetTutorials;
    }

    /// <summary>
    /// 読込み完了時のイベントを設定します
    /// </summary>
    /// <param name="handle">読込んだハンドル</param>
    private void OnLoaded(AsyncOperationHandle<TutorialDatabase> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            _tutorialDatabase = handle.Result;
        }
    }
}
