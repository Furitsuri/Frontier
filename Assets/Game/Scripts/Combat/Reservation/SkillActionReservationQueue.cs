using System.Collections.Generic;
using System.Linq;

namespace Frontier.Battle
{
    /// <summary>
    /// 予約されたスキルアクションを FIFO 順で管理するキューです。
    /// SequenceFacade（実行中シーケンス）の対として、まだ実行されていない予約アクションを保持します。
    /// DIInstaller からシングルトンとして登録し、必要な箇所から注入して使用してください。
    /// </summary>
    public class SkillActionReservationQueue
    {
        private readonly Queue<SkillActionReservationData> _queue = new Queue<SkillActionReservationData>();

        public int Count => _queue.Count;
        public bool IsEmpty => _queue.Count == 0;

        /// <summary>アクション予約データをキューの末尾に追加します</summary>
        public void Enqueue( SkillActionReservationData data )
        {
            _queue.Enqueue( data );
        }

        /// <summary>キューの先頭からデータを取り出して返します</summary>
        public SkillActionReservationData Dequeue()
        {
            return _queue.Dequeue();
        }

        /// <summary>キューの先頭のデータを取り出さずに参照します</summary>
        public SkillActionReservationData Peek()
        {
            return _queue.Peek();
        }

        /// <summary>キュー内のすべてのデータを破棄します</summary>
        public void Clear()
        {
            _queue.Clear();
        }

        /// <summary>キュー内の全データを列挙します（読み取り専用）</summary>
        public IEnumerable<SkillActionReservationData> GetAll() => _queue;
    }
}
