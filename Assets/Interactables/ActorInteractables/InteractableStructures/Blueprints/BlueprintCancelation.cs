using Interaction.Core;
using System;
using System.Collections;
using UnityEngine;

namespace Interaction.ActorInteractables.Structures
{
	[RequireComponent(typeof(UITrackedProgressComponent))]
    public class BlueprintCancelation : MonoBehaviour
    {
		private const float c_cancelationDuration = 3.0f;
		private const float c_maxShakeMagnitude = 0.1f;
		private const float c_maxShakeFrequency = 15.0f;

		// Components
		private UITrackedProgressComponent m_progressComponent;

		public event Action CanceledBlueprint;

		public bool IsBeingCanceled { get; private set; }
		private Coroutine m_cancelationCoroutine;
		private float m_shakeStrength;

		private void Awake()
		{
			m_progressComponent = GetComponent<UITrackedProgressComponent>();
			m_progressComponent.IsTrackingEnabled = false;
		}

		private void OnDestroy()
		{
			if(m_cancelationCoroutine != null)
			{
				StopCoroutine(m_cancelationCoroutine);
				m_cancelationCoroutine = null;
			}
		}

		public void BeginCancelation()
		{
			IsBeingCanceled = true;

			if (m_cancelationCoroutine != null)
				StopCoroutine(m_cancelationCoroutine);

			m_cancelationCoroutine = StartCoroutine(ShakeCoroutine(c_cancelationDuration,
				c_maxShakeMagnitude,
				c_maxShakeFrequency));
		}

		public void StopCancelation()
		{
			IsBeingCanceled = false;

			m_progressComponent.UpdateProgressValue(0f, 0f);

			if (m_cancelationCoroutine != null)
				StopCoroutine(m_cancelationCoroutine);
		}

		private IEnumerator ShakeCoroutine(float duration, float magnitude, float frequency)
		{
			Vector3 transformedOffset = transform.localPosition;
			float cancleTimer = 0;

			// Generate a random starting point in the Perlin noise map so shakes feel unique
			float randomSeedX = UnityEngine.Random.Range(0f, 100f);
			float randomSeedY = UnityEngine.Random.Range(0f, 100f);

			m_progressComponent.IsTrackingEnabled = true;

			while (cancleTimer < duration)
			{
				cancleTimer += Time.deltaTime;

				float progress = Mathf.Clamp01(cancleTimer / duration);
				float currentMagnitude = magnitude * progress;

				m_progressComponent.UpdateProgressValue(cancleTimer, duration); // Show timer counting down

				float noiseX = Mathf.PerlinNoise(randomSeedX + cancleTimer * frequency, 0f) * 2f - 1f;
				float noiseY = Mathf.PerlinNoise(0f, randomSeedY + cancleTimer * frequency) * 2f - 1f;

				transform.localPosition = new Vector3(
					transformedOffset.x + (noiseX * currentMagnitude),
					transformedOffset.y,
					transformedOffset.z
				);

				yield return null;
			}

			m_progressComponent.UpdateProgressValue(0f, 0f);

			// Always force reset back to the exact initial position
			transform.localPosition = transformedOffset;
			m_cancelationCoroutine = null;

			CanceledBlueprint?.Invoke();
		}
	}
}
