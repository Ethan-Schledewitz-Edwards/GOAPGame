using System;
using System.Collections.Generic;
using UnityEngine;

namespace Settlements
{
	[Serializable]
	public class Settlement
	{
		public event Action<Vector3> OnSettlementBoundsUpdated;

		public int SettlementID { get; private set; }
		public bool IsSettlementFriendly { get; private set; }
		public bool IsSettlementBuildable { get; private set; }
		public List<IStructure> SettlementStructures { get; private set; } = new List<IStructure>();

		public Settlement(int settlementID, bool isSettlementFriendly, bool isSettlementBuildable)
		{
			SettlementID = settlementID;
			IsSettlementFriendly = isSettlementFriendly;
			IsSettlementBuildable = isSettlementBuildable;
			Debug.Log($"New settlement of ID:{SettlementID} was created");
		}

		public void AddStructure(IStructure structure, out int structureID)
		{
			structureID = -1;

			if (structure == null)
				Debug.LogError($"Attempted to add a null structure to settlment of ID:{SettlementID}.");

			structure.StructureID = SettlementStructures.Count + 1;
			structureID = structure.StructureID;

			if (!SettlementStructures.Contains(structure))
				SettlementStructures.Add(structure);
		}

		public void RemoveStructure(IStructure structure)
		{
			if (structure == null)
				Debug.LogError($"Attempted to remove a null structure from settlment of ID:{SettlementID}.");

			if (SettlementStructures.Contains(structure))
				SettlementStructures.Remove(structure);
		}

		public IStructure FindNearestStructureOfType(Vector3 position, string structureType)
		{
			IStructure closest = null;
			float minDistance = float.MaxValue;

			foreach (IStructure structure in SettlementStructures)
			{
				if (structure == null)
					continue;

				if (structure.StructureTypeKey == structureType)
				{
					Vector3 structurePosition = structure.StructureObject.transform.position;
					float dist = Vector3.Distance(structurePosition, position);
					if (dist < minDistance)
					{
						minDistance = dist;
						closest = structure;
					}
				}
			}
			return closest;
		}

		public Vector3 GetSettlementCenter()
		{
			if (SettlementStructures.Count == 0)
				return Vector3.zero;

			Vector3 totalPosition = Vector3.zero;
			int validCount = 0;

			foreach (IStructure structure in SettlementStructures)
			{
				if (structure != null)
				{
					Vector3 structurePosition = structure.StructureObject.transform.position;
					totalPosition += structurePosition;
					validCount++;
				}
			}

			return validCount > 0 ? totalPosition / validCount : Vector3.zero;
		}
	}
}
