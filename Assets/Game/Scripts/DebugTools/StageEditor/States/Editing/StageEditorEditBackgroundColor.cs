using System;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    /// <summary>
    /// ステージ背景(Skybox)のグラデーションカラーを編集するクラス。
    /// Sub1/Sub2 で編集対象の色(上段/中段/下段)を前後に移動し、Confirm でその色を
    /// Unity ネイティブのカラーピッカーによりRGBまとめて編集できます
    /// (内部的には ColorPickerWindow という最小限の EditorWindow 上に EditorGUILayout.ColorField を1つ置き、
    /// そのスウォッチをクリックさせることでネイティブピッカーを開かせています。
    /// UnityEditor.ColorPicker.Show() は internal のため、公開APIのみで同じ体験を実現するための構成です)。
    /// 値を変更するたびに OwnCallback を呼び、Controller 側でリアルタイムに Skybox へ反映させます。
    /// </summary>
    public class StageEditorEditBackgroundColor : StageEditorEditBase
    {
        /// <summary>
        /// EditorGUILayout.ColorField を1つだけ置いた最小限のウィンドウ。
        /// スウォッチをクリックさせることで Unity ネイティブのカラーピッカーを呼び出します。
        /// ネイティブピッカーはドラッグ中に自身が repaint 対象とした View にしか自動で再描画をかけないため、
        /// EditorApplication.update で毎ティック明示的に Repaint() し、OnGUI を確実に回してドラッグ中の値を検知します。
        /// </summary>
        private class ColorPickerWindow : EditorWindow
        {
            private Color _color;
            private Action<Color> _onChanged;

            public static void Open( in Color initialColor, Action<Color> onChanged )
            {
                // 既に開いている自分自身のウィンドウが残っていれば閉じてから開き直す(多重起動防止)
                foreach( var existing in Resources.FindObjectsOfTypeAll<ColorPickerWindow>() )
                {
                    existing.Close();
                }

                var window = CreateInstance<ColorPickerWindow>();
                window._color      = initialColor;
                window._onChanged  = onChanged;
                window.titleContent = new GUIContent( "背景色" );
                window.minSize = window.maxSize = new Vector2( 240, 44 );
                window.ShowUtility();
            }

            private void OnEnable() => EditorApplication.update += Repaint;
            private void OnDisable() => EditorApplication.update -= Repaint;

            private void OnGUI()
            {
                var newColor = EditorGUILayout.ColorField( GUIContent.none, _color, true, false, false );
                if( newColor != _color )
                {
                    _color = newColor;
                    _onChanged?.Invoke( newColor );
                }
            }
        }

        public override string GetSub12Label() => "PREV/NEXT\nCOLOR";

        public override void Init( Action<EditActionContext> callback )
        {
            base.Init( callback );

            // このモードに入った時点の色をそのまま(念のため)反映させておく
            OwnCallback( _context );
        }

        public override bool CanAcceptConfirm() { return true; }
        public override bool CanAcceptSub1() { return 0 < _refParams.SelectedBackgroundColorParamIndex; }
        public override bool CanAcceptSub2() { return _refParams.SelectedBackgroundColorParamIndex < StageEditorController.StageEditRefParams.BackgroundColorParamNames.Length - 1; }

        /// <summary>
        /// 選択中の色(上段/中段/下段)を、Unityネイティブのカラーピッカーでまとめて編集します。
        /// ピッカー操作中はコールバックが継続的に呼ばれるため、ドラッグに合わせて Skybox がリアルタイムに更新されます。
        /// </summary>
        public override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            int selectedIndex = _refParams.SelectedBackgroundColorParamIndex;
            Color currentColor = selectedIndex switch
            {
                0 => _refParams.BgTopColor,
                1 => _refParams.BgMiddleColor,
                _ => _refParams.BgBottomColor,
            };

            ColorPickerWindow.Open( currentColor, pickedColor =>
            {
                _refParams.SetBackgroundColorGroup( selectedIndex, pickedColor );
                OwnCallback( _context );
            } );

            return true;
        }

        public override bool AcceptSub1( InputContext context )
        {
            if( !base.AcceptSub1( context ) ) { return false; }

            _refParams.SelectedBackgroundColorParamIndex = Math.Max( 0, _refParams.SelectedBackgroundColorParamIndex - 1 );
            return true;
        }

        public override bool AcceptSub2( InputContext context )
        {
            if( !base.AcceptSub2( context ) ) { return false; }

            _refParams.SelectedBackgroundColorParamIndex = Math.Min( StageEditorController.StageEditRefParams.BackgroundColorParamNames.Length - 1, _refParams.SelectedBackgroundColorParamIndex + 1 );
            return true;
        }
    }
}

#endif // UNITY_EDITOR
