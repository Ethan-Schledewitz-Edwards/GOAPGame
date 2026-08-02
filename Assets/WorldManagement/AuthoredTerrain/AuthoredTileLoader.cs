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

		private Dictionary<Vector2Int, GameObject> m_chunkDictionary = new Dictionary<Vector2Int, GameObject>();

		private void Awake()
		{
			m_chunkManager = GetComponent<TerrainChunkManager>();

			foreach (GameObject chunkObj in m_sceneChunks)
			{
				if (chunkObj != null)
				{
					Vector2Int chunkXZ = CoordinateUtility.WorldToChunkXZ(chunkObj.transform.position);
					m_chunkDictionary[chunkXZ] = chunkObj;
					chunkObj.SetActive(false);
				}
			}
		}

		private void OnEnable()
		{
			if (m_chunkManager.BuilderMethod != TerrainChunkManager.EChunkBuilderMethod.Authored)
				return;

			m_chunkManager.OnProcessChunkSpawn += LoadAuthoredChunk;
		}

		private void OnDisable()
		{
			if (m_chunkManager.BuilderMethod != TerrainChunkManager.EChunkBuilderMethod.Authored)
				return;

			m_chunkManager.OnProcessChunkSpawn -= LoadAuthoredChunk;
		}

		private IEnumerator LoadAuthoredChunk
			(
				Vector2Int chunkXZ,
				HashSet<Vector2Int> requestedChunks,
				HashSet<Vector2Int> pendingChunks,
				Action<Vector2Int> chunkUpdated,
				Action<TerrainChunk, GameObject> chunkFound
			)
		{
			requestedChunks.Add(chunkXZ);

			TerrainChunk chunkData = WorldManager.OnRequestChunkData?.Invoke(chunkXZ);

			if (chunkData == null)
			{
				chunkData = new TerrainChunk(chunkXZ, null, null);
			}

			chunkData.SetGenerationState(TerrainChunk.EChunkGenerationState.Decorated);
			chunkData.OnChunkUpdate += chunkUpdated;

			// Lookup the chunk
			if (m_chunkDictionary.TryGetValue(chunkXZ, out GameObject chunkObject))
			{
				chunkObject.SetActive(true);
			}
			else
			{
				Debug.LogWarning($"Authored chunk object '{chunkXZ}' not found in the Dictionary.");

				chunkObject = new GameObject($"Chunk({chunkXZ.x}, {chunkXZ.y})");
				chunkObject.transform.position = new Vector3(chunkXZ.x * WorldManager.s_ChunkSize.x, 0f, chunkXZ.y * WorldManager.s_ChunkSize.z);
			}

			chunkFound?.Invoke(chunkData, chunkObject);
			requestedChunks.Remove(chunkXZ);

			yield break;
		}
	}
}
