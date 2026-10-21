using Interaction.Core;
using NoddTracking.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;

namespace UISystem.TrackingUI
{
	public class NoddRequirmentTrackingUIManager: TrackingUIManagerBase<NoddStorageComponent, NoddRequirmentTrackingUIElement>
	{
		[SerializeField] private NoddRequirmentTrackingUIElement m_actorLimitPrefab;
		[SerializeField] private Transform m_uiCanvasParent;

		private IObjectPool<NoddRequirmentTrackingUIElement> m_uiPool;

		private Dictionary<NoddStorageComponent, System.Action> m_updateCallbacks = 
			new Dictionary<NoddStorageComponent, System.Action>();

		private void Awake()
		{
			Assert.IsNotNull(m_actorLimitPrefab, "[NoddRequirmentTrackingUIManager] The reference to the Wheel Element Prefab is missing.");

			// Why does an object pool need to be so dumb to setup John Unity?
			m_uiPool = new ObjectPool<NoddRequirmentTrackingUIElement>
			(
				createFunc: () => Instantiate(m_actorLimitPrefab, m_uiCanvasParent),
				actionOnGet: (uiElement) => uiElement.gameObject.SetActive(true),
				actionOnRelease: (uiElement) => uiElement.gameObject.SetActive(false),
				actionOnDestroy: (uiElement) => { if (uiElement != null) Destroy(uiElement.gameObject); },
				collectionCheck: true,
				defaultCapacity: 10,
				maxSize: 30
			);
		}

		protected override void RegisterTrackableSubscriptions(NoddStorageComponent noddStorage)
		{
			System.Action updateAction = () =>
			{
				if (m_activeElements.TryGetValue(noddStorage, out var uiElement))
				{
					RefreshElement(noddStorage, uiElement);
				}
			};

			m_updateCallbacks[noddStorage] = updateAction;
			noddStorage.ObjectInfoUpdated += updateAction;
		}

		protected override void CleanupTrackableSubscriptions(NoddStorageComponent noddStorage)
		{
			if (m_updateCallbacks.TryGetValue(noddStorage, out var updateAction))
			{
				noddStorage.ObjectInfoUpdated -= updateAction;
				m_updateCallbacks.Remove(noddStorage);
			}
		}

		protected override NoddRequirmentTrackingUIElement GetUIElement(NoddStorageComponent noddStorage)
		{
			return m_uiPool.Get();
		}

		protected override void ReleaseUIElement(NoddRequirmentTrackingUIElement uiElement)
		{
			m_uiPool.Release(uiElement);
		}

		protected override void InitializeUI(NoddStorageComponent noddStorage, NoddRequirmentTrackingUIElement uiElement)
		{
			//int actorsPresent = actorCounter.ActorsPresent;
			//int minimumActors = actorCounter.MinimumActors;
			//uiElement.SetActorValues(actorsPresent, minimumActors);
		}

		protected override void UpdateUI(NoddStorageComponent noddStorage, NoddRequirmentTrackingUIElement uiElement)
		{
			//int actorsPresent = actorCounter.ActorsPresent;
			//int minimumActors = actorCounter.MinimumActors;
			//uiElement.SetActorValues(actorsPresent, minimumActors);
		}
	}
}
