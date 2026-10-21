using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UISystem.TrackingUI
{
	public class ActorCounterTrackingUIElement : TrackingUIElementBase
	{
		[SerializeField] private TextMeshProUGUI m_presentText;
		[SerializeField] private TextMeshProUGUI m_minimumText;

		public void SetActorValues(int actorsPresent, int minActors)
		{
			if (m_presentText != null)
				m_presentText.text = actorsPresent.ToString();

			if (m_minimumText != null)
				m_minimumText.text = minActors.ToString();
		}

		public override void ElementReleased()
		{
			gameObject.SetActive(true);
		}

		public override void ElementReturned()
		{
			ResetDynamicPosition();
			SetActorValues(0, 0);
			gameObject.SetActive(false);
		}
	}
}
