using ObjectTags;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NoddTracking.Core
{
	public class NoddStorageComponent : MonoBehaviour
	{
		public event Action ResourcesAchieved;

		private bool m_isAchieved = false;

		private NoddQuantity[] m_requiredResources;

		private Dictionary<ResourceTypeTag, HashSet<Transform>> m_storedResources;

		private void OnDestroy()
		{
			ReleaseStoredNodds();
		}

		public void SetRequiredResources(NoddQuantity[] requiredResources)
		{
			m_requiredResources = requiredResources;
			m_isAchieved = false;
			InitializeStoredResources();
		}

		private void InitializeStoredResources()
		{
			if (m_storedResources == null)
			{
				m_storedResources = new Dictionary<ResourceTypeTag, HashSet<Transform>>();
			}
			else
			{
				m_storedResources.Clear();
			}

			if (m_requiredResources == null) 
				return;

			foreach (NoddQuantity i in m_requiredResources)
			{
				if (!m_storedResources.ContainsKey(i.TypeRequired))
				{
					m_storedResources[i.TypeRequired] = new HashSet<Transform>();
				}
			}
		}

		public bool TryAddNodd(ResourceTypeTag noddType, Transform noddTransform)
		{
			if (noddTransform == null)
			{
				Debug.LogWarning("[NoddRequestComponent] Tried to add a null Nodd", this);
				return false;
			}

			if (m_storedResources != null && m_storedResources.ContainsKey(noddType))
			{
				bool added = m_storedResources[noddType].Add(noddTransform);

				noddTransform.gameObject.SetActive(false);

				if (added && !m_isAchieved)
				{
					RefreshTrackedNodds();
				}
				return true;
			}

			return false;
		}

		public void ReleaseStoredNodds()
		{
			if (m_storedResources == null) 
				return;

			foreach (var kvp in m_storedResources)
			{
				foreach (Transform noddTransform in kvp.Value)
				{
					if (noddTransform != null)
					{
						noddTransform.gameObject.SetActive(true);
					}
				}
			}

			m_storedResources.Clear();
			m_isAchieved = false;

			// Re-initialize dictionary structures for the current requirements
			InitializeStoredResources();
		}

		private void RefreshTrackedNodds()
		{
			if (m_requiredResources == null) 
				return;

			foreach (NoddQuantity noddQuantity in m_requiredResources)
			{
				if (!m_storedResources.ContainsKey(noddQuantity.TypeRequired) ||
					m_storedResources[noddQuantity.TypeRequired].Count < noddQuantity.AmountRequired)
					return;
			}

			m_isAchieved = true;
			ResourcesAchieved?.Invoke();
		}
	}
}
