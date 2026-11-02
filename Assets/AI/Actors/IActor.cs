using UnityEngine;

namespace AI.Actors
{
    public interface IActor
    {
		public bool IsActorActive { get; }

		public void TickBehaviour(float tps);

	}
}