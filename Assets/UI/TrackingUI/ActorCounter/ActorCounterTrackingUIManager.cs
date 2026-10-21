using Interaction.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;

namespace UISystem.TrackingUI
{
	public class ActorCounterTrackingUIManager : TrackingUIManagerBase<UITrackedActorCounter, ActorCounterTrackingUIElement>
	{
		[SerializeField] private ActorCounterTrackingUIElement m_actorLimitPrefab;
		[SerializeField] private Transform m_uiCanvasParent;

		private IObjectPool<ActorCounterTrackingUIElement> m_uiPool;

		private Dictionary<UITrackedActorCounter, System.Action> m_updateCallbacks = 
			new Dictionary<UITrackedActorCounter, System.Action>();

		private void Awake()
		{
			Assert.IsNotNull(m_actorLimitPrefab, "[ActorLimitTrackingUIManager] The reference to the Wheel Element Prefab is missing.");

			// Why does an object pool need to be so dumb to setup John Unity?
			m_uiPool = new ObjectPool<ActorCounterTrackingUIElement>
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

		protected override void RegisterTrackableSubscriptions(UITrackedActorCounter actorCounter)
		{
			System.Action updateAction = () =>
			{
				if (m_activeElements.TryGetValue(actorCounter, out var uiElement))
				{
					RefreshElement(actorCounter, uiElement);
				}
			};

			m_updateCallbacks[actorCounter] = updateAction;
			actorCounter.ObjectInfoUpdated += updateAction;
		}

		protected override void CleanupTrackableSubscriptions(UITrackedActorCounter actorCounter)
		{
			if (m_updateCallbacks.TryGetValue(actorCounter, out var updateAction))
			{
				actorCounter.ObjectInfoUpdated -= updateAction;
				m_updateCallbacks.Remove(actorCounter);
			}
		}

		protected override ActorCounterTrackingUIElement GetUIElement(UITrackedActorCounter actorCounter)
		{
			return m_uiPool.Get();
		}

		protected override void ReleaseUIElement(ActorCounterTrackingUIElement uiElement)
		{
			m_uiPool.Release(uiElement);
		}

		protected override void InitializeUI(UITrackedActorCounter actorCounter, ActorCounterTrackingUIElement uiElement)
		{
			int actorsPresent = actorCounter.ActorsPresent;
			int minimumActors = actorCounter.MinimumActors;
			uiElement.SetActorValues(actorsPresent, minimumActors);
		}

		protected override void UpdateUI(UITrackedActorCounter actorCounter, ActorCounterTrackingUIElement uiElement)
		{
			int actorsPresent = actorCounter.ActorsPresent;
			int minimumActors = actorCounter.MinimumActors;
			uiElement.SetActorValues(actorsPresent, minimumActors);
		}
	}
}
