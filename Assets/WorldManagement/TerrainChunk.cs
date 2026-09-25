using SaveLoad.Core;
using SaveLoad.Data;
using System.Collections.Generic;
using UnityEngine;

namespace WorldManagement.Core
{
	[System.Serializable]
	public class TerrainChunk
	{
		public Vector2Int ChunkXZ { get; private set; }

		public List<SerializableEntityData> PendingSavables;
		public HashSet<GameObject> ResidentEntities { get; private set; } = new HashSet<GameObject>();

		public TerrainChunk(Vector2Int chunkXZ)
		{
			ChunkXZ = chunkXZ;
		}

		public void RegisterEntity(GameObject entity)
		{
			if (entity == null)
				return;

			ResidentEntities.Add(entity);
		}

		public void UnregisterEntity(GameObject entity)
		{
			if (entity == null)
				return;

			ResidentEntities.Remove(entity);
		}
	}
}