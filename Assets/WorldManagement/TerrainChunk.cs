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

		public void SetTilesID(Vector3Int localPos, int newID)
		{
			TileData[localPos.x, localPos.y, localPos.z] = newID;
			UpdateChunk();

			//// Update neighbour chunks if the updated tile was on a boarder
			//Vector3Int[] intercadinalDirs = WorldPropertyUtility.CardinalIntercardinalDirections3D;

			//for (int i = 0; i < intercadinalDirs.Length; i++)
			//{
			//	if (!TerrainQueryUtility.IsNeighborTileInChunk(ChunkXZ, TileData, localPos, intercadinalDirs[i], out Vector2Int neighbourXZ))
			//	{
			//		if (WorldGenerator.s_ActiveChunks.TryGetValue(neighbourXZ, out var neighbour))
			//		{
			//			neighbour.chunkData.UpdateChunk();
			//		}
			//	}
			//}
		}

		public void RegisterEntity(GameObject entity)
		{
			if (entity == null)
				return;

			ResidentEntities.Add(entity);

			foreach (var item in ResidentEntities)
			{
				Debug.Log(item.name + " Entered chunk");
			}
		}

		public void UnregisterEntity(GameObject entity)
		{
			if (entity == null)
				return;

			ResidentEntities.Remove(entity);
		}
	}
}