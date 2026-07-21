using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Frontier.UI
{
    /// <summary>
    /// スライダー形式のオプション項目View。
    /// 現在値をパーセンテージとして項目右端に表示します。
    /// </summary>
    public class SliderOptionItemUI : OptionItemUIBase
    {
        [SerializeField] private Slider _slider;
        [SerializeField] private TextMeshProUGUI _valueText;

        public event Action<float> OnValueChanged;

        public override void Setup()
        {
            base.Setup();

            _slider.wholeNumbers = true;
            _slider.onValueChanged.AddListener( OnSliderValueChanged );
        }

        public void SetRange( float minValue, float maxValue )
        {
            _slider.minValue = minValue;
            _slider.maxValue = maxValue;
        }

        public void SetValueWithoutNotify( float value )
        {
            _slider.SetValueWithoutNotify( value );
            UpdateValueText( value );
        }

        /// <summary>
        /// 現在値からdelta分だけ変更します(範囲内にクランプされます)。
        /// キーボード・パッドからの操作に用います
        /// </summary>
        public void AdjustValue( float delta )
        {
            _slider.value = Mathf.Clamp( _slider.value + delta, _slider.minValue, _slider.maxValue );
        }

        private void OnSliderValueChanged( float value )
        {
            UpdateValueText( value );
            OnValueChanged?.Invoke( value );
        }

        private void UpdateValueText( float value )
        {
            _valueText.text = $"{( int ) value}%";
        }
    }
}
