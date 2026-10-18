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
					m_runtimeTagSet = new HashSet<ResourceTypeTag> { m_resourceTypeTag };
				}
				return m_runtimeTagSet;
			}
		}
	}
}
