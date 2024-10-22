using Frontier.Stage;
using System.Collections.Generic;
using UnityEngine;

namespace Frontier
{
    public class PhaseStateBase : TreeNode<PhaseStateBase>
    {
        private bool _isBack = false;
        public int TransitIndex { get; protected set; } = -1;
        protected BattleManager _btlMgr = null;
        protected StageController _stageCtrl = null;

        // 初期化
        virtual public void Init(BattleManager btlMgr, StageController stgCtrl)
        {
            _btlMgr         = btlMgr;
            _stageCtrl      = stgCtrl;
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
        virtual public void UpdateKeyGuide()
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
            _btlMgr.SetDiedCharacterKey(characterKey);
        }

        /// <summary>
        /// ガイドを新たに追加します
        /// </summary>
        /// <param name="addGuide"></param>
        protected void AddKeyGuide(KeyGuideUI.KeyGuide addGuide )
        {

        }

        /// <summary>
        /// ステートの遷移に併せてキーガイドを変更します
        /// </summary>
        /// <param name="keyGuideList">遷移先のキーガイドリスト</param>
        protected void TransitKeyGuides( List<KeyGuideUI.KeyGuide> keyGuideList )
        {
            GeneralUISystem.Instance.TransitKeyGuide( keyGuideList );
        }

        /// <summary>
        /// ステートの遷移に併せてキーガイドを変更します
        /// </summary>
        /// <param name="args">遷移先で表示するキーガイド群</param>
        protected void TransitKeyGuides(params (Constants.KeyIcon, string)[] args)
        {
            List<KeyGuideUI.KeyGuide> keyGuideList = new List<KeyGuideUI.KeyGuide>();
            foreach(var arg in args ){
                keyGuideList.Add(new KeyGuideUI.KeyGuide(arg));
            }

            GeneralUISystem.Instance.TransitKeyGuide(keyGuideList);
        }
    }
}