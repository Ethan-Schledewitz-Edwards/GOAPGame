using System;
using UnityEngine;

namespace Interaction.Core
{
	public class UITrackedProgressComponent : MonoBehaviour, ITrackableObject
	{
		public Vector3 LocalTrackingPointOffset => Vector3.up * 2;

		public Transform ObjectTransform => transform;

		public event Action ObjectInfoUpdated;
		public event Action ObjectDestroyed;

		public float ProgressPercentage { get; private set; }

		private void OnDestroy()
		{
			ObjectDestroyed?.Invoke();
		}

		public void UpdateProgressValue(float value, float maxValue)
		{
			ProgressPercentage = value / maxValue;
			ObjectInfoUpdated?.Invoke();
		}
	}
}
