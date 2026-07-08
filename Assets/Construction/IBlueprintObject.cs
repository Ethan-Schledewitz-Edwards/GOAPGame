using System;
using UnityEngine;

public interface IBlueprintObject
{
	public event Action<IBlueprintObject> BlueprintStarted;

	public event Action<IBlueprintObject> BlueprintCompleted;

	public event Action<IBlueprintObject> BlueprintCanceled;

	public int SettlementID { get; } // The settlement this blueprint belongs to
	public int SettlementBlueprintID { get; } // What blueprint this is within the settlement
	public int StructureBlueprintID { get; } // The asset this blueprint represents

	public void HandleBlueprintStarted
		(
			int settlementID, 
			int settlementBlueprintID, 
			StructureBlueprintData structureBlueprintData,
			Vector3 position,
			Quaternion rotation
		);

	public void HandleBlueprintCompleted();
	public void HandleBlueprintCanceled();
}
