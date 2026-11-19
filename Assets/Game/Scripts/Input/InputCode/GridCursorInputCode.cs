using static Constants;

/// <summary>
/// ステージ上のグリッドカーソルを方向入力で動かす入力コードを生成します。
/// タイル選択・キャラクターの移動・スキルの対象タイル選択・配置など、グリッドカーソルを動かす操作は
/// 全てここで生成することで、受付インターバルと押しっぱなし時のリピート開始遅延
/// (InputRepeatConfig の GridCursor 項目)を共通に揃えます。
/// </summary>
public static class GridCursorInputCode
{
    /// <summary>
    /// グリッドカーソル移動用の入力コードを生成します
    /// </summary>
    /// <param name="explanation">入力ガイドに表示する説明文</param>
    /// <param name="enableCb">入力受付判定のコールバック</param>
    /// <param name="acceptDirection">方向入力を受け付けた際のコールバック</param>
    /// <param name="hashCode">コード登録を行ったクラスのハッシュ値</param>
    public static InputCode Create( string explanation, InputCode.EnableCallback enableCb, AcceptContextInput.AcceptContextInputCallback acceptDirection, int hashCode )
    {
        return new InputCode( GuideIcon.ALL_CURSOR, explanation, enableCb, new AcceptContextInput( acceptDirection ), GRID_DIRECTION_INPUT_INTERVAL, hashCode )
        {
            RepeatDelayType = InputRepeatDelayType.GridCursor
        };
    }
}
