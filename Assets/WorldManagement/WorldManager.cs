using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldManagement.Core
{
	[RequireComponent(typeof(TerrainChunkManager))]
	public class WorldManager : MonoBehaviour
	{
		// Signleton
		public static WorldManager Instance { get; private set; }
		public static readonly int s_Seed = 64;
		public static readonly Vector3Int s_ChunkSize = new Vector3Int(16, 32, 16);

		// Components
		private TerrainChunkManager m_chunkBuilder;

		// Events
		public static Func<Vector2Int, TerrainChunk> OnRequestChunkData;
		public static Action<TerrainChunk> OnReleaseChunkData;

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

			yield return StartCoroutine(m_chunkBuilder.BuildChunk(chunkXZ, s_requestedChunks, s_pendingChunks, OnChunkUpdate));
		}

		public IEnumerator LoadChunkBatch(Vector2Int[] chunkCoords)
		{
			foreach (Vector2Int coord in chunkCoords)
			{
				yield return StartCoroutine(LoadNewChunk(coord));
			}
		}

		public void RemoveActiveChunk(Vector2Int chunkXZ)
		{
			if (!s_ActiveChunks.TryGetValue(chunkXZ, out var activeChunkTuple))
				return;

			if (s_requestedChunks.Contains(chunkXZ))
				s_requestedChunks.Remove(chunkXZ);

			activeChunkTuple.chunkData.OnChunkUpdate -= OnChunkUpdate;

			if (activeChunkTuple.gameObject != null)
			{
				Destroy(activeChunkTuple.gameObject);
			}

			if (activeChunkTuple.chunkData.ChunkGenerationState == TerrainChunk.EChunkGenerationState.Decorated)
				OnReleaseChunkData?.Invoke(activeChunkTuple.chunkData);

			s_ActiveChunks.Remove(chunkXZ);
		}

		public void OnChunkUpdate(Vector2Int chunkXZ)
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