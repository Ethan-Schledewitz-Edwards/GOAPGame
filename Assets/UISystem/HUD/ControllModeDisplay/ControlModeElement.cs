using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ControlModeElement : HUDElement
{
	[SerializeField] private TextMeshProUGUI m_controlModeText;
	[SerializeField] private Image m_controlModeSprite;

	public override void SetPlayer(Player player)
    {
		base.SetPlayer(player);
		m_player.PlayerWorldControllerManager.ControlModeChanged += OnControllModeChanged;
	}

	private void OnDestroy()
	{
		m_player.PlayerWorldControllerManager.ControlModeChanged -= OnControllModeChanged;
	}

	private void OnControllModeChanged(PlayerWorldControllerBase controller)
	{
		m_controlModeText.text = controller.ControllerName;
		m_controlModeSprite.sprite = controller.ControllerIcon;
	}
}
