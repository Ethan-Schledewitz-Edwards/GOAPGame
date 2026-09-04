using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WorldManagement.Core;

namespace WorldManagement.AuthoredTiles
{
	[RequireComponent(typeof(TerrainChunkManager))]
    public class AuthoredTileLoader : MonoBehaviour
    {
		[SerializeField] private GameObject[] m_sceneChunks;

		private TerrainChunkManager m_chunkManager;

		private Dictionary<Vector2Int, GameObject> m_loadedAuthoredChunks = new Dictionary<Vector2Int, GameObject>();

		private void Awake()
		{
			m_chunkManager = GetComponent<TerrainChunkManager>();

			foreach (GameObject chunkObj in m_sceneChunks)
			{
				if (chunkObj != null)
				{
					Vector2Int chunkXZ = CoordinateUtility.WorldToChunkXZ(chunkObj.transform.position);
					m_loadedAuthoredChunks[chunkXZ] = chunkObj;
					chunkObj.SetActive(false);
				}
			}
		}

		private void OnEnable()
		{
			if (m_chunkManager.BuilderMethod != TerrainChunkManager.EChunkBuilderMethod.Authored)
				return;

			m_chunkManager.ProcessChunkSpawned += HandleSpawnedChunk;
		}

		private void OnDisable()
		{
			if (m_chunkManager.BuilderMethod != TerrainChunkManager.EChunkBuilderMethod.Authored)
				return;

			m_chunkManager.ProcessChunkSpawned -= HandleSpawnedChunk;
		}

		private IEnumerator HandleSpawnedChunk
			(
				Vector2Int chunkXZ,
				HashSet<Vector2Int> requestedChunks,
				HashSet<Vector2Int> pendingChunks,
				Action<Vector2Int> chunkUpdated,
				Action<TerrainChunk, GameObject> chunkFound
			)
		{
			// Ignore aut of bounds requests
			if(!m_loadedAuthoredChunks.TryGetValue(chunkXZ, out GameObject chunkObject))
				yield break;

			requestedChunks.Add(chunkXZ);

			TerrainChunk chunkData = WorldManager.OnRequestChunkData?.Invoke(chunkXZ);

			if (chunkData == null)
			{
				chunkData = new TerrainChunk(chunkXZ, null, null);
			}

			chunkData.SetGenerationState(TerrainChunk.EChunkGenerationState.Decorated);
			chunkData.OnChunkUpdate += chunkUpdated;

			// Add to active chunks
			chunkFound?.Invoke(chunkData, chunkObject);

			// Enable the object and its children
			if (chunkObject != null)
			{
				chunkObject.SetActive(true);
			}

			requestedChunks.Remove(chunkXZ);
			yield break;
		}
	}
}
