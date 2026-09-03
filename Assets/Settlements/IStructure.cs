using UnityEngine;
using ObjectTags;

namespace Settlements
{
	public interface IStructure
	{
		public StructureTag StructureTypeTag { get; }
		public int SettlementID { get; }
		public int SettlementStructureID { get; }
		public GameObject Object { get; }

		public void HandleAddedToSettlement(int settlementID, int settlementStructureID);
	}
}