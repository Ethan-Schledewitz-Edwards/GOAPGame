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

		public static Dictionary<Vector2Int, GameObject> s_AuthoredChunks = new Dictionary<Vector2Int, GameObject>();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticData()
		{
			s_AuthoredChunks.Clear();
		}

		private void Awake()
		{
			s_AuthoredChunks.Clear();
			foreach (GameObject chunkObj in m_sceneChunks)
			{
				if (chunkObj != null)
				{
					Vector2Int chunkXZ = CoordinateUtility.WorldToChunkXZ(chunkObj.transform.position);
					s_AuthoredChunks[chunkXZ] = chunkObj;
					chunkObj.SetActive(false);
				}
			}
		}

		private void Start()
		{
			m_chunkManager = GetComponent<TerrainChunkManager>();

			bool isAuthoredWorld = TerrainChunkManager.s_BuilderMethod ==
				TerrainChunkManager.EChunkBuilderMethod.Authored;

			if (isAuthoredWorld)
				m_chunkManager.ProcessChunkSpawned += HandleSpawnedChunk;
		}

		private void OnDisable()
		{
			m_chunkManager.ProcessChunkSpawned -= HandleSpawnedChunk;
		}

		private void OnDestroy()
		{
			s_AuthoredChunks.Clear();
		}

		private IEnumerator HandleSpawnedChunk(Vector2Int chunkXZ,
				HashSet<Vector2Int> requestedChunks,
				HashSet<Vector2Int> pendingChunks,
				Action<Vector2Int> chunkUpdated,
				Action<TerrainChunk, GameObject> chunkFound)
		{
			// Ignore aut of bounds requests
			if(!s_AuthoredChunks.TryGetValue(chunkXZ, out GameObject chunkObject))
				yield break;

			requestedChunks.Add(chunkXZ);

			TerrainChunk chunkData = WorldManager.OnRequestChunkData?.Invoke(chunkXZ);

			if (chunkData == null)
			{
				chunkData = new TerrainChunk(chunkXZ);
			}

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
