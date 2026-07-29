/// <summary>
/// 複数のセーブスロットを扱うセーブ/ロード処理の共通インターフェース。
/// ISaveHandler{T}(単一ファイルの設定値等を扱う)との違いは、スロット番号を指定して
/// 複数のセーブデータを個別に保存・読込できる点。
/// 実装をPC/モバイル向け(ファイルI/O)からNintendo Switch/PlayStation等の専用セーブデータAPIへ
/// 差し替える場合も、このインターフェースを介して利用する呼び出し側のコードは変更不要になる。
/// </summary>
public interface ISlotSaveHandler<T>
{
    bool Exists( int slot );
    void Save( int slot, T data );
    T Load( int slot );
    void Delete( int slot );
}
