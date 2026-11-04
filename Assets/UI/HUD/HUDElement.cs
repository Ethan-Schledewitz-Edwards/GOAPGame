using UnityEngine;
using Player.Core;
using UISystem.Core;

namespace UISystem.HUD
{
	public class HUDElement : UIElement
	{
		protected PlayerEntity m_player;

		public virtual void SetPlayer(PlayerEntity player)
		{
			m_player = player;
		}
	}
}
