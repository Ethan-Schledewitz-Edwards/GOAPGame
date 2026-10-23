using System;
using UnityEngine;

namespace Interaction.ActorInteractables.Structures
{
	[Serializable]
	public struct NoddDuplicationData
	{
		public int NoddsProduced;
		public int DuplicationDuration;
		public Transform NoddPrefab;
	}

	public interface IDuplicatableResource
	{
		public NoddDuplicationData[] NoddDuplicationData { get; }
	}
}
