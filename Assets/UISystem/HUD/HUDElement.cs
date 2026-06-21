using UnityEngine;

public class HUDElement : UIElement
{
	protected Player m_player;

    public virtual void SetPlayer(Player player)
	{
		m_player = player;
	}
}
