using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SaveLoad.Data;

namespace WorldManagement.Core
{
	[RequireComponent(typeof(TerrainChunkManager))]
	public class WorldManager : MonoBehaviour
	{
		// Signleton
		public static WorldManager Instance { get; private set; }
		public static readonly int s_Seed = 64;
		public static readonly Vector3Int s_ChunkSize = new Vector3Int(32, 32, 32);

		// Components
		private TerrainChunkManager m_chunkBuilder;

		// Events
		public static Func<Vector2Int, TerrainChunk> OnRequestChunkData;
		public static Action<TerrainChunk> OnReleaseChunkData;
		public static Action<TerrainChunk, List<SerializableEntityData>> ChunkSpawnedEntities;

		[Header("System")]
		public static Dictionary<Vector2Int, (TerrainChunk chunkData, GameObject gameObject)> s_ActiveChunks =
		new Dictionary<Vector2Int, (TerrainChunk chunkData, GameObject gameObject)>();

		private static readonly HashSet<Vector2Int> s_requestedChunks = new HashSet<Vector2Int>();
		private static readonly HashSet<Vector2Int> s_pendingChunks = new HashSet<Vector2Int>();

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;
			m_chunkBuilder = GetComponent<TerrainChunkManager>();
		}

		public IEnumerator LoadNewChunk(Vector2Int chunkXZ)
		{
			if ((s_ActiveChunks.TryGetValue(chunkXZ, out var activeChunk) && activeChunk.gameObject != null) || s_requestedChunks.Contains(chunkXZ))
				yield break;

			// Wait for the new chunk to load
			yield return StartCoroutine(m_chunkBuilder.SpawnChunk(chunkXZ, s_requestedChunks, s_pendingChunks, HandleChunkUpdated));

			// Spawn and initialize any entities saved in the chunk after it has been completely loaded
			if (s_ActiveChunks.TryGetValue(chunkXZ, out var activeChunkTuple))
			{
				if (activeChunkTuple.chunkData.PendingSavables != null && activeChunkTuple.chunkData.PendingSavables.Count > 0)
				{
					ChunkSpawnedEntities?.Invoke(activeChunkTuple.chunkData, activeChunkTuple.chunkData.PendingSavables);
					activeChunkTuple.chunkData.PendingSavables = null;
				}
			}
		}

		public IEnumerator LoadChunkBatch(Vector2Int[] chunkCoords)
		{
			foreach (Vector2Int coord in chunkCoords)
			{
				yield return StartCoroutine(LoadNewChunk(coord));
			}
		}

		public void RemoveActiveChunk(Vector2Int chunkXZ, bool shouldSave = true)
		{
			if (!s_ActiveChunks.TryGetValue(chunkXZ, out var chunk))
				return;

			if (s_requestedChunks.Contains(chunkXZ))
				s_requestedChunks.Remove(chunkXZ);

			chunk.chunkData.OnChunkUpdate -= HandleChunkUpdated;

			if (m_chunkBuilder.BuilderMethod == TerrainChunkManager.EChunkBuilderMethod.Procedural && chunk.gameObject != null)
			{
				Destroy(chunk.gameObject);
			}
			else if (chunk.gameObject != null)
			{
				chunk.gameObject.SetActive(false);
			}

			if (shouldSave && chunk.chunkData.ChunkGenerationState == TerrainChunk.EChunkGenerationState.Decorated)
			{
				OnReleaseChunkData?.Invoke(chunk.chunkData);
			}

			s_ActiveChunks.Remove(chunkXZ);
		}

		public void ClearAllChunks(bool shouldSave = true)
		{
			List<Vector2Int> keys = new List<Vector2Int>(s_ActiveChunks.Keys);
			foreach (var key in keys)
			{
				RemoveActiveChunk(key, shouldSave);
			}
		}

		public void HandleChunkUpdated(Vector2Int chunkXZ)
		{
			if (s_ActiveChunks.TryGetValue(chunkXZ, out var activeChunk) && activeChunk.gameObject != null && !s_requestedChunks.Contains(chunkXZ))
			{
				m_chunkBuilder.RebuildChunkMesh(chunkXZ, activeChunk.chunkData, activeChunk.gameObject);
			}
		}

		public static TerrainChunk GetChunkData(Vector2Int chunkXZ)
		{
			if (s_ActiveChunks.TryGetValue(chunkXZ, out var active))
				return active.chunkData;

			return OnRequestChunkData?.Invoke(chunkXZ);
		}
	}
}