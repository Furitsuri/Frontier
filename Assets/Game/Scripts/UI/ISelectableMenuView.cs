namespace Frontier.UI
{
    /// <summary>
    /// 複数の選択肢を持つメニューの見た目状態を切り替えるための共通インターフェース。
    /// 「カーソルで選択中の項目をハイライトする」「いずれかの項目を確定し、他の項目を
    /// 選択不可であることが分かる見た目にする」という2つの操作を抽象化することで、
    /// 実際の表現方法(文字色を変える、アイコンを出す、枠線を付ける等)を呼び出し側から
    /// 切り離す。表現方法を変更したい場合は、このインターフェースの実装クラス側のみを
    /// 差し替えれば良い。
    /// </summary>
    public interface ISelectableMenuView
    {
        /// <summary>
        /// 通常のカーソル選択状態を反映します(indexの項目のみ選択中の見た目、他は通常の見た目)。
        /// ロック状態を解除する目的でも使用できます。
        /// </summary>
        void SetSelectedIndex( int index );

        /// <summary>
        /// activeIndexの項目が確定され、他の項目が選択不可であることが分かる見た目にします
        /// (例: 他項目の文字をグレーアウトする)。
        /// </summary>
        void SetLockedIndex( int activeIndex );
    }
}
