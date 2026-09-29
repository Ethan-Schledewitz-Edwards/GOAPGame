using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldManagement.Core
{
    public class TerrainChunkManager : MonoBehaviour
    {
		public static TerrainChunkManager Instance { get; private set; }

		public static EChunkBuilderMethod s_BuilderMethod { get; private set; }
		[SerializeField] private EChunkBuilderMethod m_builderMethod;
		public enum EChunkBuilderMethod
		{
			Procedural,
			Authored
		}

		public delegate IEnumerator SpawnChunkDelegate(Vector2Int chunkXZ,
				HashSet<Vector2Int> requestedChunks,
				HashSet<Vector2Int> pendingChunks,
				Action<Vector2Int> chunkUpdatedCallback,
				Action<TerrainChunk, GameObject> chunkCompletedCallback);

		public SpawnChunkDelegate ProcessChunkSpawned;
		public Action<Vector2Int, TerrainChunk, GameObject> ProcessChunkRebuilt;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticState()
		{
			Instance = null;
			s_BuilderMethod = default;
		}

		private void Awake()
		{
			if (Instance != null && Instance != this)
			{
				Destroy(gameObject);
				return;
			}

			Instance = this;
			s_BuilderMethod = m_builderMethod;
		}

		private void OnDestroy()
		{
			Instance = null;
			s_BuilderMethod = default;
		}

		public IEnumerator SpawnChunk(Vector2Int chunkXZ, 
			HashSet<Vector2Int> requestedChunks, 
			HashSet<Vector2Int> pendingChunks, 
			Action<Vector2Int> onChunkUpdateCallback)
		{
			if (ProcessChunkSpawned != null)
			{
				TerrainChunk finalData = null;
				GameObject finalObject = null;

				// Ask the active generator/loader for a chunk then return the paired data and object
				yield return StartCoroutine(ProcessChunkSpawned(chunkXZ,
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

					finalObject.SetActive(true);
				}
			}
			else
			{
				Debug.LogWarning($"[TerrainChunkManager] No chunk generator/loader is " +
					$"listening for BuilderMethod: {s_BuilderMethod}", this);
				requestedChunks.Remove(chunkXZ);
			}
		}

		public void RebuildChunkMesh(Vector2Int chunkXZ, TerrainChunk chunkData, GameObject chunkObject)
		{
			ProcessChunkRebuilt?.Invoke(chunkXZ, chunkData, chunkObject);
		}
	}
}
