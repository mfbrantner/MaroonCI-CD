using GEAR.Localization.Text;
using UnityEngine;
using Vendor.SimpleTooltip;

namespace Maroon.UI
{
    [RequireComponent(typeof(SimpleTooltip))]
    public class LocalizedSimpleTooltip : LocalizedTextBase
    {
        private SimpleTooltip _tooltip;

        private new void Start()
        {
            _tooltip = GetComponent<SimpleTooltip>();
            base.Start();
        }
        public override void UpdateLocalizedText()
        {
            if (_tooltip == null)
                return;
            _tooltip.infoLeft = GetText();
        }
    }
}