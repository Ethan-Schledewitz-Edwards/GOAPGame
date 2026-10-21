using TMPro;
using UnityEngine;

namespace UISystem.TrackingUI
{
    public class TypeRequirementElement : MonoBehaviour
    {
		[SerializeField] private TextMeshProUGUI m_presentText;
		[SerializeField] private TextMeshProUGUI m_requiredText;

		public void SetActorValues(int actorsPresent, int requiredActors)
		{
			if (m_presentText != null)
				m_presentText.text = actorsPresent.ToString();

			if (m_requiredText != null)
				m_requiredText.text = requiredActors.ToString();
		}
	}
}
