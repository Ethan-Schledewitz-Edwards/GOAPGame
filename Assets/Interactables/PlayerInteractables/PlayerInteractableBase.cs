using Interaction.Core;
using System;
using UnityEngine;

namespace Interaction.Player
{
	public abstract class PlayerInteractableBase : InteractableBase
	{
		public abstract bool CanPlayerInteract { get; }
	}
}
