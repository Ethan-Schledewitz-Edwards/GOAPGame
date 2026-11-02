using UnityEngine;

namespace Interaction.Core
{
    public interface ICarryingInteractor : IInteractor
    {
		void BeginCarrying(Transform parent, Vector3 localPosition);

		void EndCarrying();
	}
}
