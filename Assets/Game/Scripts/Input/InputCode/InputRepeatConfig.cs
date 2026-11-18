using UnityEngine;
using static Constants;

/// <summary>
/// 押しっぱなし入力のリピート開始遅延の種別です。
/// InputCode.RepeatDelayType に指定することで、InputRepeatConfig 上の対応する値が使用されます。
/// </summary>
public enum InputRepeatDelayType
{
    Default = 0,    // 個別の種別を指定していない全ての入力
    PlayerMove,     // 戦闘中のプレイヤーキャラクターの移動操作(単体移動・グループ移動)
}

/// <summary>
/// 押しっぱなし入力のリピートに関する設定です(Inspector上で調整するためのアセット)。
/// InputCode から Resources.Load で読み込んで参照されます。
/// 値は参照の都度アセットから読み出すため、Play中にInspectorで変更した値も即座に反映されます。
/// </summary>
[CreateAssetMenu( fileName = "InputRepeatConfig", menuName = "Frontier/Input/InputRepeatConfig" )]
public class InputRepeatConfig : ScriptableObject
{
    // Resources配下からの読み込みパス
    public const string RESOURCE_PATH = "Config/InputRepeatConfig";

    [Header( "押しっぱなしでリピート入力が始まるまでの遅延時間(秒)" )]
    [Tooltip( "個別の種別を指定していない全ての入力(メニュー・カーソル操作等)" )]
    [Min( 0f )]
    [SerializeField] private float _defaultRepeatDelay      = DIRECTION_INPUT_REPEAT_DELAY;
    [Tooltip( "戦闘中のプレイヤーキャラクターの移動操作(単体移動・グループ移動)" )]
    [Min( 0f )]
    [SerializeField] private float _playerMoveRepeatDelay   = PLAYER_MOVE_INPUT_REPEAT_DELAY;

    private static InputRepeatConfig _instance  = null;
    private static bool _isLoaded               = false;

    /// <summary>
    /// 指定種別の、押しっぱなし時のリピート開始までの遅延時間(秒)を取得します。
    /// アセットが見つからない場合は Constants の既定値を返します。
    /// </summary>
    public static float GetRepeatDelay( InputRepeatDelayType type )
    {
        var config = GetInstance();

        switch( type )
        {
            case InputRepeatDelayType.PlayerMove:
                return ( config != null ) ? config._playerMoveRepeatDelay : PLAYER_MOVE_INPUT_REPEAT_DELAY;
            default:
                return ( config != null ) ? config._defaultRepeatDelay : DIRECTION_INPUT_REPEAT_DELAY;
        }
    }

    private static InputRepeatConfig GetInstance()
    {
        if( !_isLoaded )
        {
            _isLoaded   = true;
            _instance   = Resources.Load<InputRepeatConfig>( RESOURCE_PATH );
            if( _instance == null )
            {
                Debug.LogError( $"[InputRepeatConfig] 入力リピート設定が見つかりません: Resources/{RESOURCE_PATH}" );
            }
        }

        return _instance;
    }
}
