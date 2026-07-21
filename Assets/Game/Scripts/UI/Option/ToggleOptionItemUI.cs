using System;
using UnityEngine;
using UnityEngine.UI;

namespace Frontier.UI
{
    /// <summary>
    /// ON/OFF切替形式のオプション項目View。
    /// </summary>
    public class ToggleOptionItemUI : OptionItemUIBase
    {
        [SerializeField] private Toggle _toggle;

        public event Action<bool> OnValueChanged;

        public override void Setup()
        {
            base.Setup();

            _toggle.onValueChanged.AddListener( value => OnValueChanged?.Invoke( value ) );
        }

        public void SetValueWithoutNotify( bool value )
        {
            _toggle.SetIsOnWithoutNotify( value );
        }
    }
}
