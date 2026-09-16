using System;
using Frontier.TroopEdit;

namespace Frontier.FormTroop
{
    /// <summary>
    /// RecruitGridConfirmStateBase派生Stateに渡すコンテキストです。
    /// 親State(RecruitEmployState/RecruitDismissState)へGetParent&lt;T&gt;()で遡ってアクセスする
    /// 代わりに、必要なデータ(グリッド・トグル済み人数)と確定処理のコールバックをここに含めて渡します。
    /// </summary>
    public class RecruitGridConfirmContext
    {
        public int ToggledCount { get; }
        public TroopGridController GridController { get; }
        public Action CommitCallback { get; }

        public RecruitGridConfirmContext( int toggledCount, TroopGridController gridController, Action commitCallback )
        {
            ToggledCount   = toggledCount;
            GridController = gridController;
            CommitCallback = commitCallback;
        }
    }
}
