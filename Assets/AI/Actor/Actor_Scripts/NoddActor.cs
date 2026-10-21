using Interaction.Core;
using NoddTracking.Core;
using ObjectTags;
using System.Collections.Generic;
using UnityEngine;

namespace AI.Actors
{
    public class NoddActor : Actor, ITaggable<ResourceTypeTag>
	{
		// ITaggableProperties
		[SerializeField] private ResourceTypeTag m_resourceTypeTag;

		private HashSet<ResourceTypeTag> m_runtimeTagSet;
		public HashSet<ResourceTypeTag> RuntimeTagSet
		{
			get
			{
				if (m_runtimeTagSet == null)
				{
					m_runtimeTagSet = new HashSet<ResourceTypeTag>();

					if (m_resourceTypeTag != null)
						m_runtimeTagSet.Add(m_resourceTypeTag);
				}
				return m_runtimeTagSet;
			}
		}

		public override void UseInteractable(IInteractable interactable)
		{
			if(interactable.InteractableTransform.TryGetComponent(out NoddDepositComponent storage))
			{
				if(storage.TryAddNodd(m_resourceTypeTag, transform))
					return;
			}

			base.UseInteractable(interactable);
		}
	}
}
