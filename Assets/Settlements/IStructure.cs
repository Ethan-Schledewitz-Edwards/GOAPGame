using UnityEngine;
using ObjectTags;

namespace Settlements
{
	public interface IStructure
	{
		public StructureTag StructureTypeTag { get; }
		public int SettlementID { get; set; }
		public int SettlementStructureID { get; set; }
		public GameObject StructureObject { get; }
		public int MaxCapacity { get; }
		public int ActorsAssigned { get; }
	}

	public interface IStructure<T> : IStructure where T : IStructure<T>
	{
		void AssignActor(out T structure);
	}
}