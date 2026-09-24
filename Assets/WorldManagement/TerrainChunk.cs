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
		public int[,,] TileData { get; private set; }
		public int[,] BiomeMap { get; private set; }

		public List<SerializableEntityData> PendingSavables;
		public HashSet<GameObject> ResidentEntities { get; private set; } = new HashSet<GameObject>();

		public EChunkGenerationState ChunkGenerationState { get; private set; }
		public enum EChunkGenerationState
		{
			Empty,
			BaseTerrain,
			Decorated
		}

		public System.Action<Vector2Int> OnChunkUpdate;

		public TerrainChunk(Vector2Int chunkXZ, int[,,] tileData, int[,] biomeMap)
		{
			ChunkXZ = chunkXZ;
			TileData = tileData;
			BiomeMap = biomeMap;

			ChunkGenerationState = EChunkGenerationState.Empty;
		}

		public void SetGenerationState(EChunkGenerationState newGenerationState)
		{
			ChunkGenerationState = newGenerationState;
		}

		public void UpdateChunk()
		{
			OnChunkUpdate?.Invoke(ChunkXZ);
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