using Interaction.Core;
using UnityEngine;
using UnityEngine.Assertions;

namespace UISystem.TrackingUI
{
	public class ProgressWheelTrackingUIManager : TrackingUIManagerBase<UITrackedProgressComponent, ProgressWheelTrackingUIElement>
	{
		[SerializeField] private ProgressWheelTrackingUIElement m_wheelElementPrefab;

		private void Awake()
		{
			Assert.IsNotNull(m_wheelElementPrefab, "[ProgressWheelTrackingUIManager] The reference to the " +
				"Wheel Element Prefab is missing.");
		}

		protected override ProgressWheelTrackingUIElement GetUIElement(UITrackedProgressComponent progressComponent)
		{
			Debug.Log("Get interact prompt");

			return m_wheelElementPrefab;
		}

		protected override void ReleaseUIElement(ProgressWheelTrackingUIElement uiElement)
		{
			Debug.Log("Release");

			uiElement.gameObject.SetActive(false);
		}

		protected override void InitializeUI(UITrackedProgressComponent progressComponent, ProgressWheelTrackingUIElement uiElement)
		{
			Debug.Log("Set interact prompt");

			uiElement.SetFillValue(0f, 0f);
		}

		protected override void UpdateUI(UITrackedProgressComponent progressComponent, ProgressWheelTrackingUIElement uiElement)
		{
			float progressPercentage = progressComponent.ProgressPercentage;
			float progressValue = progressPercentage * 360f;

			uiElement.SetFillValue(progressValue, 360f);
		}
	}
}
