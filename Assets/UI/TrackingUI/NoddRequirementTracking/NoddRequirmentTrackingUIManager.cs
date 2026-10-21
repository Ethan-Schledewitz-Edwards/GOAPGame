using Interaction.Core;
using NoddTracking.Core;
using ObjectTags;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;

namespace UISystem.TrackingUI
{
	public class NoddRequirmentTrackingUIManager: TrackingUIManagerBase<NoddDepositComponent, NoddRequirmentTrackingUIElement>
	{
		[SerializeField] private NoddRequirmentTrackingUIElement m_actorLimitPrefab;
		[SerializeField] private Transform m_uiCanvasParent;

		private IObjectPool<NoddRequirmentTrackingUIElement> m_uiPool;

		private Dictionary<NoddDepositComponent, System.Action> m_updateCallbacks = 
			new Dictionary<NoddDepositComponent, System.Action>();

		private void Awake()
		{
			Assert.IsNotNull(m_actorLimitPrefab, "[NoddRequirmentTrackingUIManager] The reference to the ActorLimitPrefab is missing.");

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

		protected override void RegisterTrackableSubscriptions(NoddDepositComponent noddStorage)
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

		protected override void CleanupTrackableSubscriptions(NoddDepositComponent noddStorage)
		{
			if (m_updateCallbacks.TryGetValue(noddStorage, out var updateAction))
			{
				noddStorage.ObjectInfoUpdated -= updateAction;
				m_updateCallbacks.Remove(noddStorage);
			}
		}

		protected override NoddRequirmentTrackingUIElement GetUIElement(NoddDepositComponent noddStorage)
		{
			return m_uiPool.Get();
		}

		protected override void ReleaseUIElement(NoddRequirmentTrackingUIElement uiElement)
		{
			m_uiPool.Release(uiElement);
		}

		protected override void InitializeUI(NoddDepositComponent noddStorage, NoddRequirmentTrackingUIElement uiElement)
		{
			UpdateResourceElement(noddStorage, uiElement);
		}

		protected override void UpdateUI(NoddDepositComponent noddStorage, NoddRequirmentTrackingUIElement uiElement)
		{
			UpdateResourceElement(noddStorage, uiElement);
		}

		private void UpdateResourceElement(NoddDepositComponent noddStorage, NoddRequirmentTrackingUIElement uiElement)
		{
			if (noddStorage == null || uiElement == null)
				return;

			var requiredResources = noddStorage.RequiredResources;
			var storedResources = noddStorage.StoredResources;

			List<ResourceRequirementData> requirementsList = new List<ResourceRequirementData>();

			if (requiredResources != null)
			{
				foreach (var noddQty in requiredResources)
				{
					int presentCount = 0;
					if (storedResources != null && storedResources.TryGetValue(noddQty.TypeRequired, out var storedSet))
					{
						presentCount = storedSet.Count;
					}

					requirementsList.Add(new ResourceRequirementData
					{
						ResourceIcon = null,
						PresentCount = presentCount,
						RequiredCount = noddQty.AmountRequired
					});
				}
			}

			uiElement.DisplayRequiredResources(requirementsList);
		}
	}
}
