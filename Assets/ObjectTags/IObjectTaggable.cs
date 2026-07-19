using System.Collections.Generic;
using UnityEngine;

namespace ObjectTags
{
	public interface IObjectTaggable
	{
		HashSet<ObjectTag> RuntimeTagSet { get; }

		public bool HasTag(ObjectTag tagToCheck)
		{
			if (tagToCheck == null || RuntimeTagSet == null) 
				return false;

			return RuntimeTagSet.Contains(tagToCheck);
		}
	}
}