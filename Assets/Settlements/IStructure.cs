using UnityEngine;
using ObjectTags;

namespace Settlements
{
	public interface IStructure
	{
		public StructureTag StructureTypeTag { get; }
		public int SettlementID { get; }
		public int SettlementStructureID { get;}
		public GameObject StructureObject { get; }
		public int MaxCapacity { get; }
		public int ActorsAssigned { get; }

		public void AddStructureToSettlement(int settlementID, int settlementStructureID);
	}

	public interface IStructure<T> : IStructure where T : IStructure<T>
	{
		void AssignActor(out T structure);
	}
}