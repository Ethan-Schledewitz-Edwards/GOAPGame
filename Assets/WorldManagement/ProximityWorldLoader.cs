using SaveLoad.Core;
using SaveLoad.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldManagement.Core
{
	public class ProximityTerrainLoader : MonoBehaviour
	{
		[SerializeField] private Transform m_player;
		[SerializeField] private int m_renderDist = 4;

		private WorldManager m_worldBuilder;
		private readonly HashSet<Vector2Int> m_chunksToUnload = new HashSet<Vector2Int>();
		private readonly List<Vector2Int> m_chunksToLoad = new List<Vector2Int>();

		private Vector2Int m_lastPlayerChunk = new Vector2Int(int.MinValue, int.MinValue);
		private bool m_isDirty = false;
		private bool m_isProcessing = false;

		public event Action OnTerrainFinishedLoading;

		private void Awake()
		{
			m_worldBuilder = GetComponent<WorldManager>();
		}

		private void Start()
		{
			LoadBatchOfChunks();
		}

		private void OnEnable()
		{
			SaveEvents.GameLoaded += HandleGameLoaded;
		}

		private void OnDisable()
		{
			SaveEvents.GameLoaded -= HandleGameLoaded;
		}

		private void Update()
		{
			if (m_isProcessing || m_player == null)
				return;

			int currentChunkX = Mathf.FloorToInt(m_player.position.x / WorldManager.s_ChunkSize.x);
			int currentChunkZ = Mathf.FloorToInt(m_player.position.z / WorldManager.s_ChunkSize.z);
			Vector2Int currentChunk = new Vector2Int(currentChunkX, currentChunkZ);

			// Only recalculate when the player moves across a chunk boundary
			if (currentChunk != m_lastPlayerChunk)
			{
				m_lastPlayerChunk = currentChunk;
				LoadBatchOfChunks();
			}
		}

		private void LoadBatchOfChunks()
		{
			m_chunksToUnload.Clear();
			foreach (var activeChunk in WorldManager.s_ActiveChunks)
			{
				m_chunksToUnload.Add(activeChunk.Key);
			}

			m_chunksToLoad.Clear();
			Vector2Int[] nearbyChunks = ChunkUtility.GetChunkCoordinatesInRadius(m_player.position, m_renderDist);

			for (int i = 0; i < nearbyChunks.Length; ++i)
			{
				Vector2Int chunkCoord = nearbyChunks[i];

				if (!WorldManager.s_ActiveChunks.ContainsKey(chunkCoord))
				{
					m_chunksToLoad.Add(chunkCoord);
				}

				m_chunksToUnload.Remove(chunkCoord);
			}

			if (m_chunksToLoad.Count > 0)
			{
				m_isDirty = true;
				StartCoroutine(LoadProcess(nearbyChunks));
			}
			else if (m_isDirty)
			{
				m_isDirty = false;
				OnTerrainFinishedLoading?.Invoke();
			}

			foreach (Vector2Int chunkXZ in m_chunksToUnload)
			{
				m_worldBuilder.RemoveActiveChunk(chunkXZ);
			}
		}

		private void HandleGameLoaded(SerializablePlayerData data)
		{
			if (data == null) 
				return;

			StopAllCoroutines();
			m_isProcessing = false;

			m_worldBuilder.ClearAllChunks(false);
			m_chunksToUnload.Clear();
			m_chunksToLoad.Clear();

			m_lastPlayerChunk = new Vector2Int(int.MinValue, int.MinValue);
		}
		private IEnumerator LoadProcess(Vector2Int[] chunks)
		{
			m_isProcessing = true;

			yield return m_worldBuilder.LoadChunkBatch(chunks);

			m_isProcessing = false;

			// Check if the player moved after the batch is done loading
			int currentChunkX = Mathf.FloorToInt(m_player.position.x / WorldManager.s_ChunkSize.x);
			int currentChunkZ = Mathf.FloorToInt(m_player.position.z / WorldManager.s_ChunkSize.z);
			Vector2Int currentChunk = new Vector2Int(currentChunkX, currentChunkZ);

			if (currentChunk != m_lastPlayerChunk)
			{
				m_lastPlayerChunk = currentChunk;
				LoadBatchOfChunks();
			}
		}
	}
}
