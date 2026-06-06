using UnityEngine;

// Holds reference to PlayerUI singleton. Automatically set by player UI on startup.
[RequireComponent(typeof(CanvasGroup))]
public class UIElement : MonoBehaviour
{
	internal PlayerUI m_playerUI;

	protected CanvasGroup m_canvasGroup;
	protected RectTransform m_rectTransform;

	protected virtual void Awake()
	{
		m_canvasGroup = GetComponent<CanvasGroup>();
		m_rectTransform = GetComponent<RectTransform>();
	}
}