using SaveLoad.Data;
using UnityEngine;

namespace SaveLoad.Core
{
	public interface ISavableEntity
	{
		SerializableEntityData GenerateSaveData();
	}
}
