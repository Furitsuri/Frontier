using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontier;
using Zenject;

namespace Frontier.Tutorial
{
    public class TutorialFacade
    {
        [Inject] private ISaveHandler<TutorialSaveData> _saveHdlr = null;
        [Inject] private HierarchyBuilderBase _hierarchyBld = null;
        [Inject] private TutorialHandler _handler = null;

        private static readonly List<TriggerType> _pendingTriggers = new();
        private TutorialSaveData _saveData = null;  // 表示済みのトリガータイプ
        private TutorialPresenter _presenter = null;

        /// <summary>
        /// 初期化します
        /// </summary>
        public void Init()
        {
            LazyInject.GetOrCreate( ref _presenter, () => _hierarchyBld.InstantiateWithDiContainer<TutorialPresenter>( false ) );

            _saveData = _saveHdlr.Load();

            _handler.Init( _presenter );
            _presenter.Init();
        }

        /// <summary>
        /// チュートリアルの表示を試行します
        /// </summary>
        public void TryShowTutorial()
        {
            foreach( var trigger in _pendingTriggers )
            {
                if( _saveData._shownTriggers.Contains( trigger ) ) continue;

                // チュートリアルを表示
                if( _handler.ShowTutorial( trigger ) )
                {
                    // 表示済みのトリガータイプに追加、保存
                    _saveData._shownTriggers.Add( trigger );
                    _saveHdlr.Save( _saveData );
                }
            }
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