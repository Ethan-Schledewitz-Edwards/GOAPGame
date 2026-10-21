using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UISystem.TrackingUI
{
    public class TypeRequirementElement : MonoBehaviour
    {
		[SerializeField] private Image m_resourceIconImage;
		[SerializeField] private TextMeshProUGUI m_presentText;
		[SerializeField] private TextMeshProUGUI m_requiredText;

		public void SetActorValues(Sprite resourceIcon, int actorsPresent, int requiredActors)
		{
			if(m_resourceIconImage != null)
				m_resourceIconImage.sprite = resourceIcon;

			if (m_presentText != null)
				m_presentText.text = actorsPresent.ToString();

			if (m_requiredText != null)
				m_requiredText.text = requiredActors.ToString();
		}
	}
}