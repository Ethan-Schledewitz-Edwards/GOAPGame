using System;
using UnityEngine;

namespace Construction
{
	public interface IBlueprintObject
	{
		public event Action<IBlueprintObject> BlueprintCompleted;

		public event Action<IBlueprintObject> BlueprintCanceled;

		public int SettlementID { get; } // The settlement this blueprint belongs to
		public int SettlementStructureID { get; } // What blueprint this is within the settlement
		public GameObject Object { get; }

		public void HandleBlueprintStarted
			(
				BlueprintData structureBlueprintData,
				Vector3 position,
				Quaternion rotation
			);

		public void HandleBlueprintCompleted();
		public void CancleBlueprint();
	}
}
