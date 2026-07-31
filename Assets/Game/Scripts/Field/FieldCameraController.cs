using UnityEngine;

namespace Frontier.Field
{
    /// <summary>
    /// フィールドシーンでのカメラ操作を行います。
    /// 通常はマウス左ボタンを押しながらドラッグすることで、画面をつかんで動かす感覚で平行移動(パン)できます。
    /// ただし、シーン遷移直後、及び自身を表すキャラクターが移動している間は、キャラクターが画面中央に来るようカメラを合わせます
    /// (ノードに滞留している間のみ、既存のドラッグ操作で自由に焦点を移動できます)。
    /// </summary>
    public class FieldCameraController : MonoBehaviour
    {
        [SerializeField] private Camera _camera   = null;
        [SerializeField] private float  _panSpeed = 0.05f;
        [SerializeField] private float  _followSmoothTime = 0.15f;  // 移動追従時にカメラが焦点へ追いつくまでの平滑化時間(秒)

        private FieldPlayerCharacterView _followTarget   = null;
        private Vector3                  _followVelocity = Vector3.zero;  // SmoothDamp用の内部速度(呼び出し元で参照する必要はない)
        private FieldMenuHandler         _fieldMenuHandler = null;

        private void Awake()
        {
            if ( _camera == null ) _camera = Camera.main;
        }

        private void Start()
        {
            RegisterInputCodes();
        }

        private void LateUpdate()
        {
            // 移動中のみ追従させる。滞留中はドラッグ操作による自由な焦点移動を優先する
            if ( _followTarget != null && _followTarget.IsMoving )
            {
                FollowTargetSmoothly();
            }
        }

        private void RegisterInputCodes()
        {
            int hashCode = Hash.GetStableHash( nameof( FieldCameraController ) );

            InputFacade.Instance.RegisterInputCodes(
                ( new GuideIcon[] { GuideIcon.POINTER_MOVE, GuideIcon.POINTER_LEFT }, "FIELD\nMOVE",
                  CanAcceptPan, new AcceptContextInput( AcceptPanInput ), 0.0f, hashCode ) );
        }

        /// <summary>
        /// フィールドメニュー(及びそこから開くOption/Save/部隊編集等のサブ画面)が開いている間は、
        /// 画面操作と競合しないようパン入力・ガイド表示ともに受け付けません。
        /// </summary>
        private bool CanAcceptPan()
        {
            return _fieldMenuHandler == null || !_fieldMenuHandler.IsOpen;
        }

        /// <summary>
        /// カメラの追従対象を設定し、直ちに画面中央へその対象を捉えます(瞬時)。
        /// シーン遷移時(初回配置・戦闘/雇用からの帰還時)に呼び出してください。
        /// </summary>
        public void SetFollowTarget( FieldPlayerCharacterView target )
        {
            _followTarget   = target;
            _followVelocity = Vector3.zero;
            SnapToFollowTargetImmediate();
        }

        /// <summary>
        /// フィールドメニューの開閉状態を参照するためのハンドラを設定します。
        /// FieldSceneController.EnsureFieldMenuHandler()生成直後に一度だけ呼び出してください。
        /// </summary>
        public void SetFieldMenuHandler( FieldMenuHandler fieldMenuHandler )
        {
            _fieldMenuHandler = fieldMenuHandler;
        }

        /// <summary>
        /// 追従対象のXY座標をそのままカメラのXY座標に瞬時に反映し、画面中央に捉えます。
        /// Z座標(カメラの奥行き)はシーンで設定された値のまま変更しません。
        /// </summary>
        private void SnapToFollowTargetImmediate()
        {
            if ( _followTarget == null ) return;

            var targetPos = _followTarget.transform.position;
            var camPos    = _camera.transform.position;
            _camera.transform.position = new Vector3( targetPos.x, targetPos.y, camPos.z );
        }

        /// <summary>
        /// 追従対象のXY座標へ、SmoothDampで滑らかに近づけます。
        /// 滞留中にドラッグでカメラを離していた場合でも、移動開始時に瞬時に飛ぶことなく連続的に追いつきます。
        /// </summary>
        private void FollowTargetSmoothly()
        {
            var targetPos = _followTarget.transform.position;
            var camPos    = _camera.transform.position;
            var desired   = new Vector3( targetPos.x, targetPos.y, camPos.z );

            _camera.transform.position = Vector3.SmoothDamp( camPos, desired, ref _followVelocity, _followSmoothTime );
        }

        /// <summary>
        /// 左ボタンを押しながらのマウス移動量分、ドラッグ方向と逆にカメラを動かします。
        /// (画面をつかんで引っ張るような操作感にするため)
        /// </summary>
        private bool AcceptPanInput( InputContext context )
        {
            if ( !context.GetButton( GameButton.PointerLeft ) ) return false;
            if ( context.Stick.sqrMagnitude <= 0f )             return false;

            var delta = new Vector3( -context.Stick.x, -context.Stick.y, 0f ) * _panSpeed;
            _camera.transform.position += delta;

            return true;
        }
    }
}
