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

        /// <summary>
        /// 指定したキャラクターを攻撃対象としている予約が1件でも存在するかを返します。
        /// </summary>
        public bool HasReservationTargeting( CharacterKey targetKey )
        {
            return _queue.Any( d => d.AttackTargetCharaKeys.Contains( targetKey ) );
        }

        /// <summary>
        /// 指定攻撃者キーに一致する最初の予約データをキューから取り出します。
        /// 見つからなかった場合は false を返します。
        /// </summary>
        public bool TryDequeueByAttackerKey( CharacterKey key, out SkillActionReservationData result )
        {
            var list = _queue.ToList();
            int idx  = list.FindIndex( d => d.AttackerKey.Equals( key ) );
            if( idx < 0 )
            {
                result = null;
                return false;
            }
            result = list[idx];
            list.RemoveAt( idx );
            _queue.Clear();
            foreach( var item in list ) { _queue.Enqueue( item ); }
            return true;
        }

        /// <summary>
        /// 指定したキャラクターが死亡した際に、それを攻撃対象としているすべての予約を更新します。
        /// 対象を除外してもなお他の攻撃対象が残る予約はキュー内でそのまま更新し、
        /// 全滅した予約はキューから取り除いた上で戻り値として返します（強制終了処理は呼び出し側で行ってください）。
        /// </summary>
        public List<SkillActionReservationData> RemoveDeadTargetFromAll( CharacterKey deadTargetKey )
        {
            var list      = _queue.ToList();
            var exhausted = new List<SkillActionReservationData>();

            for( int i = 0; i < list.Count; ++i )
            {
                if( !list[i].AttackTargetCharaKeys.Contains( deadTargetKey ) ) { continue; }

                var updated = list[i].WithTargetRemoved( deadTargetKey );
                if( updated.HasAnyTarget )
                {
                    list[i] = updated;
                }
                else
                {
                    exhausted.Add( updated );
                    list.RemoveAt( i );
                    --i;
                }
            }

            _queue.Clear();
            foreach( var item in list ) { _queue.Enqueue( item ); }

            return exhausted;
        }
    }
}
