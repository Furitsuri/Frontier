using System;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

namespace Frontier.DebugTools.StageEditor
{
    /// <summary>
    /// ステージ背景(Skybox)のグラデーションカラーを編集するクラス。
    /// Sub1/Sub2 で編集対象の色(上段/中段/下段)を前後に移動します。
    /// このモードに入った瞬間、および Sub1/Sub2 で色を切り替えた瞬間に、Confirm を待たず
    /// Unity ネイティブのカラーピッカーが選択中の色に自動追従して開き続けます
    /// (内部的には ColorPickerWindow という最小限の EditorWindow 上に EditorGUILayout.ColorField を1つ置き、
    /// そのスウォッチをクリックさせることでネイティブピッカーを開かせています。
    /// UnityEditor.ColorPicker.Show() は internal のため、公開APIのみで同じ体験を実現するための構成です)。
    /// 値を変更するたびに OwnCallback を呼び、Controller 側でリアルタイムに Skybox へ反映させます。
    /// Confirm はユーザーがウィンドウを閉じてしまった場合の再オープン用に残しています。
    /// </summary>
    public class StageEditorEditBackgroundColor : StageEditorEditBase
    {
        /// <summary>
        /// EditorGUILayout.ColorField を1つだけ置いた最小限のウィンドウ。
        /// スウォッチをクリックさせることで Unity ネイティブのカラーピッカーを呼び出します。
        /// ネイティブピッカーはドラッグ中に自身が repaint 対象とした View にしか自動で再描画をかけないため、
        /// EditorApplication.update で毎ティック明示的に Repaint() し、OnGUI を確実に回してドラッグ中の値を検知します。
        /// 既に開いている場合はウィンドウを閉じ直さず、色とコールバックだけを差し替えます(位置が飛ばないようにするため)。
        ///
        /// このウィンドウが Game View から OS キーボードフォーカスを奪っている間、旧Input Manager経由の
        /// Q/E(Sub1/Sub2)はゲーム側に届かなくなる(Game Viewがフォーカスを失うため)。
        /// そこでこのウィンドウ自身の OnGUI 内で Event.current を見て Q/E を直接検知し、
        /// onPrevColor/onNextColor 経由でフォーカスに関係なく色の切替を反映させる。
        /// </summary>
        private class ColorPickerWindow : EditorWindow
        {
            private Color _color;
            private Action<Color> _onChanged;
            private Action _onPrevColor;
            private Action _onNextColor;

            public static void Open( string label, in Color initialColor, Action<Color> onChanged, Action onPrevColor, Action onNextColor )
            {
                var existingWindows = Resources.FindObjectsOfTypeAll<ColorPickerWindow>();
                var window = existingWindows.Length > 0 ? existingWindows[0] : null;

                if( window == null )
                {
                    window = CreateInstance<ColorPickerWindow>();
                    window.minSize = window.maxSize = new Vector2( 240, 44 );
                    window.ShowUtility();
                }

                window.titleContent = new GUIContent( $"背景色: {label}" );
                window._color       = initialColor;
                window._onChanged   = onChanged;
                window._onPrevColor = onPrevColor;
                window._onNextColor = onNextColor;
                window.Repaint();
            }

            /// <summary>開いている ColorPickerWindow があれば閉じます(モード離脱時の後始末用)。</summary>
            public static void CloseIfOpen()
            {
                foreach( var w in Resources.FindObjectsOfTypeAll<ColorPickerWindow>() )
                {
                    w.Close();
                }
            }

            private void OnEnable() => EditorApplication.update += Repaint;
            private void OnDisable() => EditorApplication.update -= Repaint;

            private void OnGUI()
            {
                HandleKeyboardShortcuts();

                var newColor = EditorGUILayout.ColorField( GUIContent.none, _color, true, false, false );
                if( newColor != _color )
                {
                    _color = newColor;
                    _onChanged?.Invoke( newColor );
                }
            }

            /// <summary>
            /// このウィンドウがフォーカスを持っている間は旧Input Manager(Game View依存)がQ/Eを拾えないため、
            /// Event.current から直接キー入力を検知して色の前後切替を発火させる。
            /// </summary>
            private void HandleKeyboardShortcuts()
            {
                var e = Event.current;
                if( e.type != EventType.KeyDown ) { return; }

                if( e.keyCode == KeyCode.Q )
                {
                    _onPrevColor?.Invoke();
                    e.Use();
                }
                else if( e.keyCode == KeyCode.E )
                {
                    _onNextColor?.Invoke();
                    e.Use();
                }
            }
        }

        public override string GetSub12Label() => "PREV/NEXT\nCOLOR";

        public override void Init( Action<EditActionContext> callback )
        {
            base.Init( callback );

            // このモードに入った時点の色をそのまま(念のため)反映させておく
            OwnCallback( _context );

            // Confirm を待たず、選択中の色のピッカーをすぐに開いて追従させる
            OpenPickerForSelectedColor();
        }

        public override void Exit()
        {
            // モードを離れたら、追従用ピッカーを開いたままにしないよう閉じる
            ColorPickerWindow.CloseIfOpen();
        }

        public override bool CanAcceptConfirm() { return true; }
        public override bool CanAcceptSub1() { return 0 < _refParams.SelectedBackgroundColorParamIndex; }
        public override bool CanAcceptSub2() { return _refParams.SelectedBackgroundColorParamIndex < StageEditorController.StageEditRefParams.BackgroundColorParamNames.Length - 1; }

        /// <summary>
        /// ユーザーがピッカーを閉じてしまった場合の再オープン用です。
        /// 通常は Init / Sub1 / Sub2 の時点で自動的にピッカーが選択中の色に追従します。
        /// </summary>
        public override bool AcceptConfirm( InputContext context )
        {
            if( !base.AcceptConfirm( context ) ) { return false; }

            OpenPickerForSelectedColor();
            return true;
        }

        public override bool AcceptSub1( InputContext context )
        {
            if( !base.AcceptSub1( context ) ) { return false; }

            SelectPreviousColor();
            return true;
        }

        public override bool AcceptSub2( InputContext context )
        {
            if( !base.AcceptSub2( context ) ) { return false; }

            SelectNextColor();
            return true;
        }

        /// <summary>
        /// 選択中の色を1つ前(Top方向)へ移動します。既に先頭の場合は何もしません。
        /// 通常の Sub1 入力に加え、ColorPickerWindow がフォーカスを持っている間の Q キー検知からも呼ばれます。
        /// </summary>
        private void SelectPreviousColor()
        {
            _refParams.SelectedBackgroundColorParamIndex = Math.Max( 0, _refParams.SelectedBackgroundColorParamIndex - 1 );
            OpenPickerForSelectedColor();
        }

        /// <summary>
        /// 選択中の色を1つ次(Bottom方向)へ移動します。既に末尾の場合は何もしません。
        /// 通常の Sub2 入力に加え、ColorPickerWindow がフォーカスを持っている間の E キー検知からも呼ばれます。
        /// </summary>
        private void SelectNextColor()
        {
            _refParams.SelectedBackgroundColorParamIndex = Math.Min( StageEditorController.StageEditRefParams.BackgroundColorParamNames.Length - 1, _refParams.SelectedBackgroundColorParamIndex + 1 );
            OpenPickerForSelectedColor();
        }

        /// <summary>
        /// 選択中の色(上段/中段/下段)を対象に、カラーピッカーを開く(または既に開いていれば追従させる)。
        /// ピッカー操作中はコールバックが継続的に呼ばれるため、ドラッグに合わせて Skybox がリアルタイムに更新されます。
        /// </summary>
        private void OpenPickerForSelectedColor()
        {
            int selectedIndex = _refParams.SelectedBackgroundColorParamIndex;
            string label = StageEditorController.StageEditRefParams.BackgroundColorParamNames[selectedIndex];
            Color currentColor = selectedIndex switch
            {
                0 => _refParams.BgTopColor,
                1 => _refParams.BgMiddleColor,
                _ => _refParams.BgBottomColor,
            };

            ColorPickerWindow.Open( label, currentColor,
                pickedColor =>
                {
                    _refParams.SetBackgroundColorGroup( selectedIndex, pickedColor );
                    OwnCallback( _context );
                },
                onPrevColor: SelectPreviousColor,
                onNextColor: SelectNextColor );
        }
    }
}

#endif // UNITY_EDITOR
