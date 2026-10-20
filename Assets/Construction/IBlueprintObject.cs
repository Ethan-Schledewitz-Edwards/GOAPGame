using System;
using System.Collections.Generic;
using UnityEngine;

namespace Construction
{
	public interface IBlueprintObject
	{
		public int SettlementID { get; } // The settlement this blueprint belongs to
		public int SettlementStructureID { get; } // What blueprint this is within the settlement
		public GameObject Object { get; }

		public event Action<IBlueprintObject> BlueprintCompleted;

		public event Action<IBlueprintObject> BlueprintCanceled;

		public void HandleBlueprintPlaced(BlueprintData structureBlueprintData,
				Vector3 position,
				Quaternion rotation);

	}

	public interface IMultiPointBlueprintObject : IBlueprintObject
	{
		public void AddPoint(Vector3 position);
		public IReadOnlyList<Vector3> GetPlacementPositions();
		public void GenerateMultiPointBlueprintLinks();
	}
}
