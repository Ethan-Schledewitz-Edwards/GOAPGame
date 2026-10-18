using ObjectTags;
using System;
using UnityEngine;

namespace NoddTracking.Core
{
	[Serializable]
	public struct NoddQuantity
	{
		public ResourceTypeTag TypeRequired;
		public int AmountRequired;
	}
}
