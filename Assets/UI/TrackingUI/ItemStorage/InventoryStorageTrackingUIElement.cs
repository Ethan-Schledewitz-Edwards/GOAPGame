using TMPro;
using UnityEngine;

public class ItemStorageTrackingUIElement : TrackingUIElementBase
{
	[SerializeField] private TextMeshProUGUI m_promptText;

	public void SetPromptText(string prompt)
	{
		if (m_promptText != null)
			m_promptText.SetText(prompt);
	}

	public override void ElementReleased()
	{
		gameObject.SetActive(true);
	}

	public override void ElementReturned()
	{
		ResetDynamicPosition();
		SetPromptText(string.Empty);
		gameObject.SetActive(false);
	}
}
