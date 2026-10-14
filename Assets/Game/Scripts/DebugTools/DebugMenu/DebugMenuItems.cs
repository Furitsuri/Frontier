using System;

#if UNITY_EDITOR

namespace Frontier.DebugTools.DebugMenu
{
    /// <summary>
    /// デバッグメニューの項目の型です。型によってメニュー上での操作方法が決まります
    /// </summary>
    public enum DebugMenuItemType
    {
        TOGGLE,     // 決定でON/OFFを切り替える
        INT,        // 左右で値を増減する
        CHOICE,     // 左右で選択肢を切り替える
        ACTION,     // 決定で処理を実行する
        READ_ONLY,  // 値の表示のみ
    }

    /// <summary>
    /// デバッグメニューの項目の基底クラスです。
    /// 項目は「ページ・名前・型・値の読み書き」の組で定義し、DebugMenuItemRegistry へ登録します。
    /// メニューからの操作(Confirm/Step)と、コードからの直接操作(各派生クラスのSet等)のどちらも同じ読み書き処理を通ります
    /// </summary>
    public abstract class DebugMenuItem
    {
        public DebugMenuPage Page { get; }
        public string Name { get; }
        public abstract DebugMenuItemType Type { get; }

        protected DebugMenuItem( DebugMenuPage page, string name )
        {
            Page        = page;
            Name        = name;
        }

        /// <summary>
        /// 現在の値を表示用の文字列で取得します
        /// </summary>
        public abstract string GetValueText();

        /// <summary>
        /// 決定入力を受けた際の処理を行います
        /// </summary>
        /// <returns>処理を行ったか</returns>
        public virtual bool Confirm() { return false; }

        /// <summary>
        /// 左右入力を受けた際の処理を行います
        /// </summary>
        /// <param name="delta">左なら-1、右なら+1</param>
        /// <returns>処理を行ったか</returns>
        public virtual bool Step( int delta ) { return false; }
    }

    /// <summary>
    /// ON/OFFを切り替える項目です
    /// </summary>
    public sealed class DebugToggleItem : DebugMenuItem
    {
        private readonly Func<bool> _getter;
        private readonly Action<bool> _setter;

        public override DebugMenuItemType Type => DebugMenuItemType.TOGGLE;

        public DebugToggleItem( DebugMenuPage page, string name, Func<bool> getter, Action<bool> setter ) : base( page, name )
        {
            _getter = getter;
            _setter = setter;
        }

        public bool Get() { return _getter(); }
        public void Set( bool value ) { _setter( value ); }

        public override string GetValueText() { return Get() ? "ON" : "OFF"; }

        public override bool Confirm()
        {
            Set( !Get() );
            return true;
        }
    }

    /// <summary>
    /// 整数値を増減する項目です。
    /// 対象によって範囲が変わる値(最大HPなど)に対応するため、最小値・最大値は関数で受け取ります
    /// </summary>
    public sealed class DebugIntItem : DebugMenuItem
    {
        private readonly Func<int> _getter;
        private readonly Action<int> _setter;
        private readonly Func<int> _min;
        private readonly Func<int> _max;

        public override DebugMenuItemType Type => DebugMenuItemType.INT;

        public DebugIntItem( DebugMenuPage page, string name, Func<int> getter, Action<int> setter, Func<int> min, Func<int> max ) : base( page, name )
        {
            _getter = getter;
            _setter = setter;
            _min    = min;
            _max    = max;
        }

        public int Get() { return _getter(); }

        /// <summary>
        /// 値を設定します。範囲外の値は範囲内に丸められます
        /// </summary>
        public void Set( int value ) { _setter( Math.Clamp( value, _min(), _max() ) ); }

        public override string GetValueText() { return Get().ToString(); }

        public override bool Step( int delta )
        {
            Set( Get() + delta );
            return true;
        }
    }

    /// <summary>
    /// 選択肢を切り替える項目です。選択肢の表現(何を何順に巡るか)は登録側に委ねます。
    /// 決定時の処理を渡した場合は、選択中の選択肢に対する決定操作も受け付けます
    /// </summary>
    public sealed class DebugChoiceItem : DebugMenuItem
    {
        private readonly Func<string> _getter;
        private readonly Action<int> _step;
        private readonly Action _confirm;

        public override DebugMenuItemType Type => DebugMenuItemType.CHOICE;

        public DebugChoiceItem( DebugMenuPage page, string name, Func<string> getter, Action<int> step, Action confirm = null ) : base( page, name )
        {
            _getter     = getter;
            _step       = step;
            _confirm    = confirm;
        }

        public override string GetValueText() { return _getter(); }

        public override bool Confirm()
        {
            if( _confirm == null ) { return false; }

            _confirm();
            return true;
        }

        public override bool Step( int delta )
        {
            _step( delta );
            return true;
        }
    }

    /// <summary>
    /// 決定で処理を実行する項目です
    /// </summary>
    public sealed class DebugActionItem : DebugMenuItem
    {
        private readonly Action _execute;

        public override DebugMenuItemType Type => DebugMenuItemType.ACTION;

        public DebugActionItem( DebugMenuPage page, string name, Action execute ) : base( page, name )
        {
            _execute = execute;
        }

        public void Execute() { _execute(); }

        public override string GetValueText() { return ""; }

        public override bool Confirm()
        {
            Execute();
            return true;
        }
    }

    /// <summary>
    /// 値を表示するのみの項目です
    /// </summary>
    public sealed class DebugReadOnlyItem : DebugMenuItem
    {
        private readonly Func<string> _getter;

        public override DebugMenuItemType Type => DebugMenuItemType.READ_ONLY;

        public DebugReadOnlyItem( DebugMenuPage page, string name, Func<string> getter ) : base( page, name )
        {
            _getter = getter;
        }

        public override string GetValueText() { return _getter(); }
    }
}

#endif // UNITY_EDITOR
