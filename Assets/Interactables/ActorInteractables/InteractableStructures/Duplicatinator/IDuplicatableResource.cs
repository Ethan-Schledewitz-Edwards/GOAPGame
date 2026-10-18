using System;
using UnityEngine;

namespace Interaction.ActorInteractables.Structures
{
	[Serializable]
	public struct NoddEnvelope
	{
		public int NoddsProduced;
		public int DuplicationDuration;
		public Transform NoddPrefab;
	}

	public interface IDuplicatableResource
	{
		public NoddEnvelope[] NoddEnvelopes { get; }
	}
}
