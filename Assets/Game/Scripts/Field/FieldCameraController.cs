using UnityEngine;

namespace Frontier.Field
{
    /// <summary>
    /// フィールドシーンでのカメラ操作を行います。
    /// マウス左ボタンを押しながらドラッグすることで、画面をつかんで動かす感覚で平行移動(パン)できます。
    /// </summary>
    public class FieldCameraController : MonoBehaviour
    {
        [SerializeField] private Camera _camera   = null;
        [SerializeField] private float  _panSpeed = 0.05f;

        private void Awake()
        {
            if ( _camera == null ) _camera = Camera.main;
        }

        private void Start()
        {
            RegisterInputCodes();
        }

        private void RegisterInputCodes()
        {
            int hashCode = Hash.GetStableHash( nameof( FieldCameraController ) );

            InputFacade.Instance.RegisterInputCodes(
                ( new GuideIcon[] { GuideIcon.POINTER_MOVE, GuideIcon.POINTER_LEFT }, "FIELD\nMOVE",
                  InputFacade.CanBeAcceptAlways, new AcceptContextInput( AcceptPanInput ), 0.0f, hashCode ) );
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
