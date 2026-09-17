using SaveLoad.Data;
using System;
using UnityEngine;

namespace SaveLoad.Core
{
	public interface ISavableEntity
	{
		public bool SavedByChunks { get; }

		public event Action DataRestored;
		public event Action<Vector3, Quaternion> TransformRestored;

		SerializableEntityData GenerateSaveData();
	}
}
