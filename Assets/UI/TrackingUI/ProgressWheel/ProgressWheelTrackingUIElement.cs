using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UISystem.TrackingUI
{
	public class ProgressWheelTrackingUIElement : TrackingUIElementBase
	{
		[SerializeField] private Image m_fillWheelImage;

		public void SetFillValue(float value, float maxValue)
		{
			if (m_fillWheelImage != null)
				m_fillWheelImage.fillAmount = value / maxValue;
		}

		public override void ElementReleased()
		{
			gameObject.SetActive(true);
		}

		public override void ElementReturned()
		{
			ResetDynamicPosition();
			SetFillValue(0f, 0);
			gameObject.SetActive(false);
		}
	}
}
