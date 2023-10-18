using Frontier.Stage;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Frontier
{
    public class CharacterParameterUI : MonoBehaviour
    {
        public enum SIDE
        {
            LEFT = 0,
            RIGHT,
            NUM,
        }

        [SerializeField]
        private TextMeshProUGUI TMPMaxHPValue;
        [SerializeField]
        private TextMeshProUGUI TMPCurHPValue;
        [SerializeField]
        private TextMeshProUGUI TMPAtkValue;
        [SerializeField]
        private TextMeshProUGUI TMPDefValue;
        [SerializeField]
        private TextMeshProUGUI TMPAtkNumValue;
        [SerializeField]
        private TextMeshProUGUI TMPDiffHPValue;
        [SerializeField]
        private TextMeshProUGUI TMPActRecoveryValue;
        [SerializeField]
        private RawImage TargetImage;
        [SerializeField]
        private RectTransform PanelTransform;
        [SerializeField]
        private RawImage ActGaugeElemImage;
        [SerializeField]
        private SkillBoxUI[] SkillBoxes;
        [SerializeField]
        private float BlinkingDuration;

        private Character _character;
        private Camera _camera;
        private RenderTexture _targetTexture;
        private List<RawImage> _actGaugeElems;
        private SIDE _side;
        private float _camareAngleY;
        private float _alpha;
        private float _blinkingElapsedTime;
        // 左右のパラメータウィンドウでカメラのレイヤー名を分ける
        // 同じレイヤー名にすると、左右のウィンドウで表示するキャラクター同士が接近した際に、互いのカメラに映り込んでしまう
        private string[] _layerNames = new string[] { Constants.LAYER_NAME_LEFT_PARAM_WINDOW, Constants.LAYER_NAME_RIGHT_PARAM_WINDOW };

        void Start()
        {
            _targetTexture = new RenderTexture((int)TargetImage.rectTransform.rect.width * 2, (int)TargetImage.rectTransform.rect.height * 2, 16, RenderTextureFormat.ARGB32);
            TargetImage.texture = _targetTexture;
            GameObject gameObject = new GameObject();
            _camera = gameObject.AddComponent<Camera>();
            _camera.enabled = false;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0, 0, 0, 0);
            _camera.targetTexture = _targetTexture;
            _actGaugeElems = new List<RawImage>(Constants.ACTION_GAUGE_MAX);

            for (int i = 0; i < Constants.ACTION_GAUGE_MAX; ++i)
            {
                var elem = Instantiate(ActGaugeElemImage);
                _actGaugeElems.Add(elem);
                elem.gameObject.SetActive(false);
                elem.transform.SetParent(PanelTransform, true);
            }
        }

        // Update is called once per frame
        void Update()
        {
            // キャラクターがnullの状態でGameObjectがActiveになっていることは想定しない
            Debug.Assert(_character != null);

            // パラメータ表示を反映
            UpdateParamRender(_character, _character.param, _character.skillModifiedParam);
            // カメラ描画を反映
            UpdateCamraRender(_character, _character.camParam);
        }

        /// <summary>
        /// パラメータUIに表示するキャラクターのパラメータを更新します
        /// </summary>
        /// <param name="selectCharacter">選択しているキャラクター</param>
        /// <param name="param">選択しているキャラクターのパラメータ</param>
        void UpdateParamRender(Character selectCharacter, in Character.Parameter param, in Character.SkillModifiedParameter skillParam)
        {
            Debug.Assert(param.consumptionActionGauge <= param.curActionGauge);

            TMPMaxHPValue.text = $"{param.MaxHP}";
            TMPCurHPValue.text = $"{param.CurHP}";
            TMPAtkValue.text = $"{param.Atk}";
            TMPDefValue.text = $"{param.Def}";
            TMPAtkNumValue.text = $"x {skillParam.AtkNum}";
            TMPActRecoveryValue.text = $"+{param.recoveryActionGauge}";
            TMPAtkNumValue.gameObject.SetActive(1 < skillParam.AtkNum);

            int changeHP = selectCharacter.tmpParam.totalExpectedChangeHP;
            changeHP = Mathf.Clamp(changeHP, -param.CurHP, param.MaxHP - param.CurHP);
            if (0 < changeHP)
            {
                TMPDiffHPValue.text = $"+{changeHP}";
            }
            else if (changeHP < 0)
            {
                TMPDiffHPValue.text = $"{changeHP}";
            }
            else
            {
                // ダメージが0の場合は表示しない
                TMPDiffHPValue.text = "";
            }

            // テキストの色を反映
            ApplyTextColor(changeHP);

            // アクションゲージの表示
            for (int i = 0; i < Constants.ACTION_GAUGE_MAX; ++i)
            {
                var elem = _actGaugeElems[i];

                if (i <= param.maxActionGauge - 1)
                {
                    elem.gameObject.SetActive(true);

                    if (i <= param.curActionGauge - 1)
                    {
                        elem.color = Color.green;

                        // アクションゲージ使用時は点滅させる
                        if ((param.curActionGauge - param.consumptionActionGauge) <= i)
                        {
                            _blinkingElapsedTime += Time.deltaTime;
                            _alpha = Mathf.PingPong(_blinkingElapsedTime / BlinkingDuration, 1.0f);
                            elem.color = new Color(0, 1, 0, _alpha);
                        }
                    }
                    else
                    {
                        elem.color = Color.gray;
                    }
                }
                else
                {
                    elem.gameObject.SetActive(false);
                }
            }

            // スキルボックスの表示
            for (int i = 0; i < Constants.EQUIPABLE_SKILL_MAX_NUM; ++i)
            {
                if (param.IsValidSkill(i))
                {
                    SkillBoxes[i].gameObject.SetActive(true);
                    string skillName = SkillsData.data[(int)param.equipSkills[i]].Name;
                    var type = SkillsData.data[(int)param.equipSkills[i]].Type;
                    SkillBoxes[i].SetSkillName(skillName, type);
                    SkillBoxes[i].ShowSkillCostImage(SkillsData.data[(int)param.equipSkills[i]].Cost);
                }
                else
                {
                    SkillBoxes[i].gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// テキストの色を反映します
        /// </summary>
        /// <param name="tmpParam">該当キャラクターの一時パラメータ</param>
        void ApplyTextColor(int changeHP)
        {
            if (changeHP < 0)
            {
                TMPDiffHPValue.color = Color.red;
            }
            else if (0 < changeHP)
            {
                TMPDiffHPValue.color = Color.green;
            }
        }


        /// <summary>
        /// パラメータUIに表示するキャラクターのカメラ描画を更新します
        /// </summary>
        /// <param name="selectCharacter">選択しているキャラクター</param>
        /// <param name="param">選択しているキャラクターのパラメータ</param>
        void UpdateCamraRender(Character selectCharacter, in Character.CameraParameter camParam)
        {
            Transform playerTransform = selectCharacter.transform;
            Vector3 add = Quaternion.AngleAxis(_camareAngleY, Vector3.up) * playerTransform.forward * camParam.UICameraLengthZ;
            _camera.transform.position = playerTransform.position + add + Vector3.up * camParam.UICameraLengthY;
            _camera.transform.LookAt(playerTransform.position + Vector3.up * camParam.UICameraLookAtCorrectY);
            _camera.Render();
        }

        /// <summary>
        /// 初期化します
        /// </summary>
        /// <param name="angleY">初期化時のY軸カメラアングル</param>
        public void Init(float angleY, SIDE side)
        {
            _camareAngleY = angleY;
            _side = side;
            // _sideが決定したのでそれに合わせたレイヤーマスクを個々で設定
            _camera.cullingMask = 1 << LayerMask.NameToLayer(_layerNames[(int)_side]);
        }

        /// <summary>
        /// 差分HP用テキストを返します
        /// </summary>
        /// <returns>差分HP用テキスト</returns>
        public TextMeshProUGUI GetDiffHPText()
        {
            return TMPDiffHPValue;
        }

        /// <summary>
        /// 指定のスキルボックスUIを取得します
        /// </summary>
        /// <param name="index"></param>
        /// <returns>指定値</returns>
        public SkillBoxUI GetSkillBox(int index)
        {
            Debug.Assert(0 <= index && index < Constants.EQUIPABLE_SKILL_MAX_NUM);

            return SkillBoxes[index];
        }

        /// <summary>
        /// 表示するキャラクターを設定します
        /// </summary>
        /// <param name="character">表示キャラクター</param>
        public void SetDisplayCharacter(Character character)
        {
            _character = character;

            // パラメータ画面表示用にキャラクターのレイヤーを変更
            _character.gameObject.SetLayerRecursively(LayerMask.NameToLayer(_layerNames[(int)_side]));
        }

        public void ClearDisplayCharacter()
        {
            _character.gameObject.SetLayerRecursively(LayerMask.NameToLayer(Constants.LAYER_NAME_CHARACTER));
        }
    }
}