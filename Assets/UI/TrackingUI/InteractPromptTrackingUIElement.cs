using TMPro;
using UnityEngine;

public class InteractPromptTrackingUIElement : TrackingUIElementBase
{
	[SerializeField] private TextMeshProUGUI m_promptText;

	public void SetPromptText(string prompt)
	{
		if (m_promptText != null)
			m_promptText.SetText(prompt);
	}
}
