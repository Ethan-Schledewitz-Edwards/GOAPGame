using System;
using UnityEngine;

namespace Interaction.Core
{
	public class UITrackedProgressComponent : MonoBehaviour, ITrackableByElement
	{
		// ITrackableObject Properties
		public bool IsTrackingEnabled { get;  set; } = false;

		public Transform ElementTrackingPosition => m_localElementTrackingPosition;
		[SerializeField] private Transform m_localElementTrackingPosition;

		public Transform ObjectTransform => transform;

		public event Action ObjectInfoUpdated;
		public event Action ObjectDestroyed;

		// System
		public float ProgressPercentage { get; private set; }

		private void OnDestroy()
		{
			ObjectDestroyed?.Invoke();
		}

		/// <summary>
		/// Calculates the progress percentage then updates any paried tracking elements.
		/// A value or max value of zero will disable tracking.
		/// </summary>
		public void UpdateProgressValue(float value, float maxValue)
		{
			if(value <= 0.0f || maxValue <= 0)
			{
				IsTrackingEnabled = false;
				return;
			}

			ProgressPercentage = value / maxValue;
			ObjectInfoUpdated?.Invoke();
		}
	}
}
