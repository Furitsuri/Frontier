using System;

public interface IServiceProvider
{
    T GetService<T>();
}

public delegate void AcceptInputCallback<T>(T arg);

public class ClassA
{
    // ジェネリックデリゲートを受け取るフィールド
    private Delegate AcceptCb;

    // ジェネリックデリゲートを設定するメソッド
    public void SetAcceptCallback<T>(AcceptInputCallback<T> arg)
    {
        AcceptCb = arg;
    }

    // コールバックを実行するメソッド
    public void ExecuteCallback<T>(T arg)
    {
        // デリゲートがセットされていれば実行
        if (AcceptCb is AcceptInputCallback<T> callback)
        {
            callback(arg);
        }
        else
        {
            Console.WriteLine("No valid callback set.");
        }
    }
}


public class ClassB
{
    public static void Main()
    {
        // クラスAのインスタンスを作成
        ClassA classA = new ClassA();

        // コールバック関数を定義
        AcceptInputCallback<int> callback = (int value) =>
        {
            Console.WriteLine($"Callback received value: {value}");
        };

        // クラスAにコールバックをセット
        classA.SetAcceptCallback(callback);

        // コールバックを実行
        classA.ExecuteCallback(42); // 結果：Callback received value: 42
    }
}