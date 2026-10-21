using Interaction.Core;
using ObjectTags;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NoddTracking.Core
{
	public class NoddDepositComponent : MonoBehaviour, ITrackableObject
	{
		// ITrackableObject Properties
		public bool IsTrackingEnabled { get; set; } = false;

		public Vector3 LocalTrackingPointOffset => Vector3.zero;

		public Transform ObjectTransform => transform;

		public event Action ObjectInfoUpdated;
		public event Action ObjectDestroyed;

		// Events
		public event Action ResourcesAchieved;

		// System
		private bool m_isAchieved = false;

		public NoddQuantity[] RequiredResources { get; private set; }
		public Dictionary<ResourceTypeTag, HashSet<Transform>> StoredResources { get; private set; }

		private void OnDestroy()
		{
			ObjectDestroyed?.Invoke();

			if (!m_isAchieved)
				ReleaseStoredNodds();
		}

		public void SetRequiredResources(NoddQuantity[] requiredResources)
		{
			RequiredResources = requiredResources;
			m_isAchieved = false;
			InitializeStoredResources();
			IsTrackingEnabled = true;
		}

		private void InitializeStoredResources()
		{
			if (StoredResources == null)
			{
				StoredResources = new Dictionary<ResourceTypeTag, HashSet<Transform>>();
			}
			else
			{
				StoredResources.Clear();
			}

			if (RequiredResources == null) 
				return;

			// Create hash sets to track for each required resource type
			foreach (NoddQuantity i in RequiredResources)
			{
				if (!StoredResources.ContainsKey(i.TypeRequired))
				{
					StoredResources[i.TypeRequired] = new HashSet<Transform>();
				}
			}

			ObjectInfoUpdated?.Invoke();
		}

		public bool TryAddNodd(ResourceTypeTag noddType, Transform noddTransform)
		{
			if (noddTransform == null)
			{
				Debug.LogWarning("[NoddRequestComponent] Tried to add a null Nodd", this);
				return false;
			}

			// Try to add a nodd transform to the specified resource
			if (StoredResources != null && StoredResources.ContainsKey(noddType))
			{
				bool added = StoredResources[noddType].Add(noddTransform);
				noddTransform.gameObject.SetActive(false);

				if (added && !m_isAchieved)
				{
					RefreshTrackedNodds();
				}

				ObjectInfoUpdated?.Invoke();
				return true;
			}

			return false;
		}

		public void ReleaseStoredNodds()
		{
			if (StoredResources == null) 
				return;

			foreach (var kvp in StoredResources)
			{
				foreach (Transform noddTransform in kvp.Value)
				{
					if (noddTransform != null)
					{
						noddTransform.gameObject.SetActive(true);
					}
				}
			}

			StoredResources.Clear();
			m_isAchieved = false;

			// Re-initialize dictionary structures for the current requirements
			InitializeStoredResources();
		}

		private void RefreshTrackedNodds()
		{
			if (RequiredResources == null) 
				return;

			foreach (NoddQuantity noddQuantity in RequiredResources)
			{
				if (!StoredResources.ContainsKey(noddQuantity.TypeRequired) ||
					StoredResources[noddQuantity.TypeRequired].Count < noddQuantity.AmountRequired)
					return;
			}

			m_isAchieved = true;
			ResourcesAchieved?.Invoke();
		}
	}
}
