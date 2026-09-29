using UnityEngine;
using Player.Core;

public class HUDElement : UIElement
{
	protected Player.Core.PlayerEntity m_player;

    public virtual void SetPlayer(Player.Core.PlayerEntity player)
	{
		m_player = player;
	}
}
