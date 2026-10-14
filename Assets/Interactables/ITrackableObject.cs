using System;
using UnityEngine;

namespace Interaction.Core
{
    public interface ITrackableObject
    {
		/// <summary>
		/// The local offset relative to the object used to position the world-space UI element.
		/// </summary>
		public Vector3 LocalTrackingPointOffset { get; }
		public Transform ObjectTransform { get; }

		public event Action ObjectInfoUpdated;
		public event Action ObjectDestroyed;
	}
}
