using NoddTracking.Core;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UISystem.TrackingUI
{
	public struct ResourceRequirementData
	{
		public Sprite ResourceIcon;
		public int PresentCount;
		public int RequiredCount;
	}

	public class NoddRequirmentTrackingUIElement : TrackingUIElementBase
	{
		[SerializeField] private TypeRequirementElement m_typeRequirementPrefab;

		// System
		private List<TypeRequirementElement> m_elementPool = new List<TypeRequirementElement>();

		public void DisplayRequiredResources(List<ResourceRequirementData> resourcesRequired)
		{
			int requiredResourceCount = resourcesRequired != null ? resourcesRequired.Count : 0;

			// Expand the pool when more elements are needed
			while (m_elementPool.Count < requiredResourceCount)
			{
				TypeRequirementElement newElement = Instantiate(m_typeRequirementPrefab, transform);
				m_elementPool.Add(newElement);
			}

			// Update active elements and deactivate any unused ones in the pool
			for (int i = 0; i < m_elementPool.Count; i++)
			{
				if (i < requiredResourceCount)
				{
					m_elementPool[i].gameObject.SetActive(true);
					ResourceRequirementData resourceData = resourcesRequired[i];

					Sprite iconSprite = resourceData.ResourceIcon != null ? resourceData.ResourceIcon : null;

					m_elementPool[i].SetActorValues(iconSprite, resourceData.PresentCount, resourceData.RequiredCount);
				}
				else
				{
					m_elementPool[i].gameObject.SetActive(false);
				}
			}
		}

		public override void ElementReleased()
		{
			gameObject.SetActive(true);
		}

		public override void ElementReturned()
		{
			ResetDynamicPosition();

			// Deactivate all pooled sub-elements
			for (int i = 0; i < m_elementPool.Count; i++)
			{
				if (m_elementPool[i] != null)
				{
					m_elementPool[i].gameObject.SetActive(false);
				}
			}

			gameObject.SetActive(false);
		}
	}
}
