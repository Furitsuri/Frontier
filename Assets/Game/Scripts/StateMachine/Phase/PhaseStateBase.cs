using Frontier.Stage;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Frontier
{
    public class PhaseStateBase : TreeNode<PhaseStateBase>
    {
        private bool _isBack = false;
        public int TransitIndex { get; protected set; } = -1;
        protected HierarchyBuilder _hierarchyBld    = null;
        protected BattleManager _btlMgr             = null;
        protected StageController _stageCtrl        = null;

        [Inject]
        public void Construct( HierarchyBuilder hierarchyBld, BattleManager btlMgr, StageController stgCtrl)
        {
            _hierarchyBld   = hierarchyBld;
            _btlMgr         = btlMgr;
            _stageCtrl      = stgCtrl;
        }

        // 初期化
        virtual public void Init()
        {
            TransitIndex    = -1;
            _isBack         = false;
        }

        // 更新
        virtual public bool Update()
        {
            if (Input.GetKeyUp(KeyCode.Backspace))
            {
                Back();

                return true;
            }

            return false;
        }

        /// <summary>
        /// キーガイドを更新します
        /// </summary>
        virtual public void UpdateInputGuide()
        {

        }

        // 退避
        virtual public void Exit()
        {
        }

        // 戻る
        virtual public bool IsBack()
        {
            return _isBack;
        }

        /// <summary>
        /// 親の遷移に戻ります
        /// </summary>
        protected void Back()
        {
            _isBack = true;
        }

        /// <summary>
        /// 死亡したキャラクターの存在を通知します
        /// </summary>
        /// <param name="characterKey">死亡したキャラクターのハッシュキー</param>
        protected void NoticeCharacterDied(CharacterHashtable.Key characterKey)
        {
            _btlMgr.BtlCharaCdr.SetDiedCharacterKey(characterKey);
        }

        /// <summary>
        /// ガイドを新たに追加します
        /// </summary>
        /// <param name="addGuide">追加するガイド</param>
        protected void AddInputGuide(InputGuideUI.InputGuide addGuide )
        {

        }

        /// <summary>
        /// ステートの遷移に併せてキーガイドを変更します
        /// </summary>
        /// <param name="keyGuideList">遷移先のキーガイドリスト</param>
        protected void SetInputGuides( List<InputGuideUI.InputGuide> keyGuideList )
        {
           //  GeneralUISystem.Instance.SetInputGuideList( keyGuideList );
        }

        /// <summary>
        /// ステートの遷移に併せてキーガイドを変更します
        /// </summary>
        /// <param name="args">遷移先で表示するキーガイド群</param>
        protected void SetInputGuides(params (Constants.KeyIcon, string)[] args)
        {
            List<InputGuideUI.InputGuide> keyGuideList = new List<InputGuideUI.InputGuide>();
            foreach(var arg in args ){
                keyGuideList.Add(new InputGuideUI.InputGuide(arg));
            }

            // GeneralUISystem.Instance.SetInputGuideList(keyGuideList);
        }

        /// <summary>
        /// TODO : 全てコールバックを登録する形でキーの受付が出来ないかの試験用
        /// </summary>
        /// <param name="args"></param>
        protected void SetInputGuides(params (Constants.KeyIcon, string, InputGuideUI.InputGuide.InputCallBack)[] args)
        {
            List<InputGuideUI.InputGuide> keyGuideList = new List<InputGuideUI.InputGuide>();
            foreach (var arg in args)
            {
                keyGuideList.Add(new InputGuideUI.InputGuide(arg));
            }

            // GeneralUISystem.Instance.SetInputGuideList(keyGuideList);
        }
    }
}