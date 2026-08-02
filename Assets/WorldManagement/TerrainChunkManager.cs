using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WorldManagement.Tiles;

namespace WorldManagement.Core
{
    public class TerrainChunkManager : MonoBehaviour
    {
		[field: SerializeField] public EChunkBuilderMethod BuilderMethod { get; private set; }
		public enum EChunkBuilderMethod
		{
			Procedural,
			Authored
		}

		public delegate IEnumerator SpawnChunkDelegate
			(
				Vector2Int chunkXZ,
				HashSet<Vector2Int> requestedChunks,
				HashSet<Vector2Int> pendingChunks,
				Action<Vector2Int> chunkUpdatedCallback,
				Action<TerrainChunk, GameObject> chunkCompletedCallback
			);

		public SpawnChunkDelegate OnProcessChunkSpawn;
		public Action<Vector2Int, TerrainChunk, GameObject> OnProcessChunkRebuild;

		public IEnumerator SpawnChunk(Vector2Int chunkXZ, HashSet<Vector2Int> requestedChunks, HashSet<Vector2Int> pendingChunks, Action<Vector2Int> onChunkUpdateCallback)
		{
			if (OnProcessChunkSpawn != null)
			{
				TerrainChunk finalData = null;
				GameObject finalObject = null;

				// Ask the active generator/loader for a chunk then return the paired data and object
				yield return StartCoroutine(OnProcessChunkSpawn(
					chunkXZ,
					requestedChunks,
					pendingChunks,
					onChunkUpdateCallback,
					(chunkData, chunkObject) =>
					{
						finalData = chunkData;
						finalObject = chunkObject;
					}));

				// Pass the pair to the World Manager
				if (finalData != null && finalObject != null)
				{
					WorldManager.s_ActiveChunks[chunkXZ] = (finalData, finalObject);
				}
			}
			else
			{
				Debug.LogWarning($"No chunk generator/loader is listening for BuilderMethod: {BuilderMethod}");
				requestedChunks.Remove(chunkXZ);
			}
		}

		public void RebuildChunkMesh(Vector2Int chunkXZ, TerrainChunk chunkData, GameObject chunkObject)
		{
			OnProcessChunkRebuild?.Invoke(chunkXZ, chunkData, chunkObject);
		}
	}
}
