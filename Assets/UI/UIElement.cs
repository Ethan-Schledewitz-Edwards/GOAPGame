using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UIElement : MonoBehaviour
{
	protected CanvasGroup m_canvasGroup;
	protected RectTransform m_parentRectTransform;
	protected RectTransform m_rectTransform;

	protected virtual void Awake()
	{
		m_canvasGroup = GetComponent<CanvasGroup>();
		m_parentRectTransform = GetComponentInParent<RectTransform>();
		m_rectTransform = GetComponent<RectTransform>();
	}
}