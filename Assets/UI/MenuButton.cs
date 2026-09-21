using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MenuButton : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
	[field: Header("Sounds")]
	[SerializeField] private AudioClip m_hoverButton;
	[SerializeField] private AudioClip m_pressButton;

	[field: Header("Properties")]

	[SerializeField, Tooltip("Plays sounds even if the button is not interactable.")]
	private bool m_forceSounds;

	// System
	protected Button m_pairedButton;
	protected Menu m_parentMenu;

	private void Awake()
	{
		m_pairedButton = GetComponent<Button>();
	}

	public void InitButton(Menu menu)
	{
		m_parentMenu = menu;
		m_pairedButton = GetComponent<Button>();
		m_pairedButton.onClick.AddListener(ButtonClicked);
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		PlaySound(m_hoverButton);
	}

	public void OnSelect(BaseEventData eventData)
	{
		PlaySound(m_pressButton);
	}

	private void PlaySound(AudioClip clip)
	{
		if (clip != null && isActiveAndEnabled && (m_forceSounds || m_pairedButton.interactable))
		{
			MenuManager.AudioSource.PlayOneShot(clip);
		}
	}

	protected virtual void ButtonClicked(){}
}