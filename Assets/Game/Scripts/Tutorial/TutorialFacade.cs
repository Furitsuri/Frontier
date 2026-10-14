using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using Zenject;

namespace Frontier.Tutorial
{
    public class TutorialFacade
    {
        [Inject] private ISaveHandler<TutorialSaveData> _saveHdlr = null;
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        
        private static readonly List<TriggerType> _pendingTriggers = new();
        private TutorialHandler _handler = null;
        private TutorialSaveData _saveData = null;  // 表示済みのトリガータイプ
        private TutorialPresenter _presenter = null;
        private Task _tutorialDataLoadTask = null;

        public void Setup( FocusRoutineBase handler )
        {
            _saveData   = _saveHdlr.Load();

            LazyInject.GetOrCreate( ref _handler, () => handler as TutorialHandler );
            LazyInject.GetOrCreate( ref _presenter, () => _hierarchyBld.InstantiateWithDiContainer<TutorialPresenter>( false ) );
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        public void Init()
        {
            _handler.Init( _presenter );
            _presenter.Init();
        }

        /// <summary>
        /// チュートリアルの表示を試行します
        /// </summary>
        public void TryShowTutorial()
        {
            // チュートリアルデータの読込が完了するまでは表示を保留する
            if( _tutorialDataLoadTask == null || !_tutorialDataLoadTask.IsCompleted ) { return; }

            for( int i = 0; i < _pendingTriggers.Count; ++i )
            {
                if( _saveData._shownTriggers.Contains( _pendingTriggers[i] ) )
                {
                    _pendingTriggers.RemoveAt( i );
                    --i;

                    continue;
                }

                // チュートリアルを表示
                if( _handler.ShowTutorial( _pendingTriggers[i] ) )
                {
                    // 表示済みのトリガータイプに追加、保存
                    _saveData._shownTriggers.Add( _pendingTriggers[i] );
                    _saveHdlr.Save( _saveData );
                }
            }
        }

        /// <summary>
        /// 読み込んだチュートリアルデータの一覧を取得します。読込みが完了していない場合は空の一覧を返します
        /// </summary>
        public IReadOnlyList<TutorialData> GetTutorialDatas()
        {
            return _handler.GetTutorialDatas();
        }

        /// <summary>
        /// チュートリアルデータの一覧の、指定番号のチュートリアルを表示します。
        /// 表示済みとしての記録は行いません(デバッグでの内容確認用)
        /// </summary>
        /// <param name="index">GetTutorialDatas()で得られる一覧の中での番号</param>
        /// <param name="onExited">表示したチュートリアルを閉じた際に一度だけ呼ばれるコールバック</param>
        /// <returns>表示の成否</returns>
        public bool ShowTutorialAt( int index, Action onExited )
        {
            return _handler.ShowTutorialAt( index, onExited );
        }

        public void LoadTutorialData()
        {
            _tutorialDataLoadTask = _handler.LoadTutorialData();
        }

        /// <summary>
        /// チュートリアルのトリガーを通知します
        /// 通知されたトリガーは、チュートリアル表示処理の際に使用されます
        /// </summary>
        /// <param name="type">通知するトリガータイプ</param>
        static public void Notify( TriggerType type )
        {
            if( !_pendingTriggers.Contains( type ) )
            {
                _pendingTriggers.Add( type );
            }
        }

        /// <summary>
        /// 通知済みのトリガーをクリアします
        /// </summary>
        static public void Clear()
        {
            _pendingTriggers.Clear();
        }

        /// <summary>
        /// チュートリアルの処理を行うハンドラを取得します
        /// </summary>
        /// <returns>ハンドラ</returns>
        public IFocusRoutine GetFocusRoutine()
        {
            return _handler;
        }
    }
}