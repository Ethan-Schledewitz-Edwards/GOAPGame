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

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticData()
		{
			Instance = null;
			ClearStaticState();
		}

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;
			ClearStaticState();
			m_chunkBuilder = GetComponent<TerrainChunkManager>();
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
				ClearStaticState();
			}
		}

		public static bool TryGetActiveChunkData(Vector2Int chunkXZ, out TerrainChunk chunkData)
		{
			if (s_ActiveChunks.TryGetValue(
				chunkXZ,
				out var activeChunk) &&
				activeChunk.chunkData != null)
			{
				chunkData = activeChunk.chunkData;
				return true;
			}

			chunkData = null;
			return false;
		}

		public IEnumerator LoadNewChunk(Vector2Int chunkXZ)
		{
			if (s_ActiveChunks.TryGetValue(chunkXZ, out var activeChunk) && activeChunk.gameObject != null)
				yield break;

			if (s_requestedChunks.Contains(chunkXZ))
				yield break;

			// Wait for the new chunk to load
			yield return StartCoroutine(m_chunkBuilder.SpawnChunk(chunkXZ, 
				s_requestedChunks, 
				s_pendingChunks, 
				HandleChunkUpdated));

			// Spawn and initialize any entities saved in the chunk after it has been completely loaded
			if (s_ActiveChunks.TryGetValue(chunkXZ, out var activeChunkTuple))
			{
				if (activeChunkTuple.gameObject != null)
					activeChunkTuple.gameObject.SetActive(true);

				TerrainChunk chunkData = activeChunkTuple.chunkData;

				if (chunkData.PendingSavables != null &&
					chunkData.PendingSavables.Count > 0)
				{
					ChunkSpawnedEntities?.Invoke(chunkData, chunkData.PendingSavables);

					chunkData.PendingSavables = null;
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

			if (shouldSave)
				OnReleaseChunkData?.Invoke(chunk.chunkData);

			if (chunk.gameObject != null)
			{
				bool isAuthoredWorld = TerrainChunkManager.s_BuilderMethod ==
					TerrainChunkManager.EChunkBuilderMethod.Authored;

				if (isAuthoredWorld)
					chunk.gameObject.SetActive(false);
				else
					Destroy(chunk.gameObject);
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
			if (s_ActiveChunks.TryGetValue(chunkXZ, out var activeChunk) &&
				activeChunk.gameObject != null &&
				!s_requestedChunks.Contains(chunkXZ))
			{
				m_chunkBuilder.RebuildChunkMesh(chunkXZ,
					activeChunk.chunkData,
					activeChunk.gameObject);
			}
		}

		public static TerrainChunk GetChunkData(Vector2Int chunkXZ)
		{
			if (TryGetActiveChunkData(chunkXZ, out TerrainChunk activeChunk))
			{
				return activeChunk;
			}

			return OnRequestChunkData?.Invoke(chunkXZ);
		}

		private static void ClearStaticState()
		{
			s_ActiveChunks.Clear();
			s_requestedChunks.Clear();
			s_pendingChunks.Clear();

			OnRequestChunkData = null;
			OnReleaseChunkData = null;
			ChunkSpawnedEntities = null;
		}
	}
}