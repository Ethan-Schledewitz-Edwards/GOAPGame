using System;
using System.Collections.Generic;
using UnityEngine;
using ObjectTags;

namespace Settlements
{
	[Serializable]
	public class Settlement
	{
		public event Action<Vector3> OnSettlementBoundsUpdated;

		public int SettlementID { get; private set; }
		public bool IsSettlementFriendly { get; private set; }
		public bool IsSettlementBuildable { get; private set; }
		public Dictionary<int, IStructure> SettlementStructures { get; private set; } = new Dictionary<int, IStructure>();
		private int m_nextAvailableID = 1;

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
			{
				Debug.LogError("Attempted to add a null structure.");
				return;
			}

			structureID = m_nextAvailableID++;
			structure.SettlementStructureID = structureID;

			SettlementStructures[structureID] = structure;
		}

		public void RemoveStructure(int structureID)
		{
			if (SettlementStructures.ContainsKey(structureID))
				SettlementStructures.Remove(structureID);
		}

		public IStructure FindNearestStructureOfType(Vector3 position, StructureTag structureTag)
		{
			IStructure closest = null;
			float minDistance = float.MaxValue;

			foreach (var structure in SettlementStructures.Values)
			{
				if (structure.StructureTypeTag == structureTag)
				{
					float dist = Vector3.Distance(structure.StructureObject.transform.position, position);
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

			foreach (var structure in SettlementStructures)
			{
				Vector3 structurePosition = structure.Value.StructureObject.transform.position;
				totalPosition += structurePosition;
				validCount++;
			}

			return validCount > 0 ? totalPosition / validCount : Vector3.zero;
		}
	}
}
