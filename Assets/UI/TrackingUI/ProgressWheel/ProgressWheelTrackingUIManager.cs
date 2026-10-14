using Interaction.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;

namespace UISystem.TrackingUI
{
	public class ProgressWheelTrackingUIManager : TrackingUIManagerBase<UITrackedProgressComponent, ProgressWheelTrackingUIElement>
	{
		[SerializeField] private ProgressWheelTrackingUIElement m_wheelElementPrefab;
		[SerializeField] private Transform m_uiCanvasParent;

		private IObjectPool<ProgressWheelTrackingUIElement> m_uiPool;

		private Dictionary<UITrackedProgressComponent, System.Action> m_updateCallbacks = 
			new Dictionary<UITrackedProgressComponent, System.Action>();

		private void Awake()
		{
			Assert.IsNotNull(m_wheelElementPrefab, "[ProgressWheelTrackingUIManager] The reference to the Wheel Element Prefab is missing.");

			// Why does an object pool need to be so dumb to setup John Unity?
			m_uiPool = new ObjectPool<ProgressWheelTrackingUIElement>
			(
				createFunc: () => Instantiate(m_wheelElementPrefab, m_uiCanvasParent),
				actionOnGet: (uiElement) => uiElement.gameObject.SetActive(true),
				actionOnRelease: (uiElement) => uiElement.gameObject.SetActive(false),
				actionOnDestroy: (uiElement) => { if (uiElement != null) Destroy(uiElement.gameObject); },
				collectionCheck: true,
				defaultCapacity: 10,
				maxSize: 30
			);
		}

		protected override void RegisterTrackableSubscriptions(UITrackedProgressComponent progressComponent)
		{
			System.Action updateAction = () =>
			{
				if (m_activeElements.TryGetValue(progressComponent, out var uiElement))
				{
					RefreshElement(progressComponent, uiElement);
				}
			};

			m_updateCallbacks[progressComponent] = updateAction;
			progressComponent.ObjectInfoUpdated += updateAction;
		}

		protected override void CleanupTrackableSubscriptions(UITrackedProgressComponent progressComponent)
		{
			if (m_updateCallbacks.TryGetValue(progressComponent, out var updateAction))
			{
				progressComponent.ObjectInfoUpdated -= updateAction;
				m_updateCallbacks.Remove(progressComponent);
			}
		}

		protected override ProgressWheelTrackingUIElement GetUIElement(UITrackedProgressComponent progressComponent)
		{
			return m_uiPool.Get();
		}

		protected override void ReleaseUIElement(ProgressWheelTrackingUIElement uiElement)
		{
			m_uiPool.Release(uiElement);
		}

		protected override void InitializeUI(UITrackedProgressComponent progressComponent, ProgressWheelTrackingUIElement uiElement)
		{
			float progressPercentage = progressComponent.ProgressPercentage;
			float progressValue = progressPercentage * 360f;

			uiElement.SetFillValue(progressValue, 360f);
		}

		protected override void UpdateUI(UITrackedProgressComponent progressComponent, ProgressWheelTrackingUIElement uiElement)
		{
			float progressPercentage = progressComponent.ProgressPercentage;
			float progressValue = progressPercentage * 360f;

			uiElement.SetFillValue(progressValue, 360f);
		}
	}
}
