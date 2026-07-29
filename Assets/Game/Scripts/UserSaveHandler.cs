using System.IO;
using UnityEngine;
using static Constants;

/// <summary>
/// ユーザーのセーブデータ(UserSaveData)をスロットごとにファイルへ保存/読込します。
/// 現状はPC/モバイル向けに Application.persistentDataPath 配下へJSONで書き出す実装のみ。
/// Nintendo Switch/PlayStation等、専用のセーブデータAPIが必要なプラットフォームに対応する際は、
/// 同じ ISlotSaveHandler{UserSaveData} を実装する別クラスに差し替える想定
/// (呼び出し側であるSaveLoadPresenter等の変更は不要)。
/// </summary>
public class UserSaveHandler : ISlotSaveHandler<UserSaveData>
{
    private readonly string[] _filePaths = new string[USER_SAVE_SLOT_COUNT];

    public UserSaveHandler()
    {
        _filePaths[USER_SAVE_AUTO_SLOT_INDEX] = Path.Combine( Application.persistentDataPath, "user_auto.json" );
        for ( int i = 0; i < USER_SAVE_SLOT_COUNT; ++i )
        {
            if ( i == USER_SAVE_AUTO_SLOT_INDEX ) continue;
            _filePaths[i] = Path.Combine( Application.persistentDataPath, $"user_slot{i}.json" );
        }
    }

    public bool Exists( int slot ) => File.Exists( _filePaths[slot] );

    public void Save( int slot, UserSaveData data )
    {
        string json = JsonUtility.ToJson( data );
        File.WriteAllText( _filePaths[slot], json );
    }

    public UserSaveData Load( int slot )
    {
        if ( !Exists( slot ) ) return null;

        string json = File.ReadAllText( _filePaths[slot] );
        return JsonUtility.FromJson<UserSaveData>( json );
    }

    public void Delete( int slot )
    {
        if ( Exists( slot ) ) File.Delete( _filePaths[slot] );
    }
}
