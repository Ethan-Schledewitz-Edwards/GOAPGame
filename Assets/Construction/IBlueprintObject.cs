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
		public int StructureBlueprintID { get; } // The asset this blueprint represents
		public GameObject BlueprintObject { get; }

		public void HandleBlueprintStarted
			(
				StructureBlueprintData structureBlueprintData,
				Vector3 position,
				Quaternion rotation
			);

		public void HandleBlueprintCompleted();
		public void HandleBlueprintCanceled();
	}
}
