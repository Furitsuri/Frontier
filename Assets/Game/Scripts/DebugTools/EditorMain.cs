using Frontier.Battle;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier.DebugTools
{
    public class EditorMain : FocusRoutineController
    {
        // 基底クラス(FocusRoutineController)の InputFacade / TutorialFacade セットアップは
        // このシーン(StageEditorScene)では不要なため、Awake() をあえてオーバーライドせず隠蔽する
        private new HierarchyBuilderBase _hierarchyBld;

        [Inject]
        public void Construct( HierarchyBuilderBase hierarchyBld )
        {
            _hierarchyBld = hierarchyBld;
        }

        new void Awake()
        {
            if (null == _hierarchyBld)
            {
                LogHelper.LogError("HierarchyBuilder is null. Please check the DI container setup.");
                return;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            base.Init();
        }

        // Update is called once per frame
        void Update()
        {
            base.UpdateRoutine();
        }

        void LateUpdate()
        {
            base.LateUpdateRoutine();
        }

        void FixedUpdate()
        {
            base.FixedUpdateRoutine();
        }
    }
}