using System;
using UnityEngine;

namespace Interaction.Core
{
	public class UITrackedActorCounter : MonoBehaviour, ITrackableObject
	{
		// ITrackableObject Properties
		public bool IsTrackingEnabled { get; set; } = false;

		public Vector3 LocalTrackingPointOffset => Vector3.up * 2;

		public Transform ObjectTransform => transform;

		public event Action ObjectInfoUpdated;
		public event Action ObjectDestroyed;

		// System
		public int ActorsPresent { get; private set; }
		public int MinimumActors { get; private set; }

		private void OnDestroy()
		{
			ObjectDestroyed?.Invoke();
		}

		public void UpdateActorValues(int actorsPresent, int minActors)
		{
			ActorsPresent = actorsPresent;
			MinimumActors = minActors;
			ObjectInfoUpdated?.Invoke();
		}
	}
}
