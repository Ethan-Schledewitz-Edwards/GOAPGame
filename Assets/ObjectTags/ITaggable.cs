using System.Collections.Generic;
using UnityEngine;

namespace ObjectTags
{
	public interface ITaggable<T>
	{
		HashSet<T> RuntimeTagSet { get; }

		public bool HasTag(T tagToCheck)
		{
			if (tagToCheck == null || RuntimeTagSet == null) 
				return false;

			return RuntimeTagSet.Contains(tagToCheck);
		}
	}
}