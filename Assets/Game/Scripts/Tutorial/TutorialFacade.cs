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

        public Task LoadTutorialData()
        {
            return _handler.LoadTutorialData();
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