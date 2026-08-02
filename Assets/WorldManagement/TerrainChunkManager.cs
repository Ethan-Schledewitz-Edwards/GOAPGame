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

		// Events
		public event Func<Vector2Int, IEnumerator> OnGenerateBaseTerrain;
		public event Action<TerrainChunk> OnDecorateChunk;
		public event Func<TerrainChunk, Action<Mesh>, IEnumerator> OnGenerateMesh;
		public event Action<TerrainChunk, Transform> OnSpawnFeatures;
		public event Action<Vector2Int, TerrainChunk, GameObject> OnRebuildMeshRequested;

		public IEnumerator BuildChunk(Vector2Int chunkXZ, HashSet<Vector2Int> requestedChunks, HashSet<Vector2Int> pendingChunks, Action<Vector2Int> onChunkUpdateCallback)
		{
			requestedChunks.Add(chunkXZ);

			for (int x = -1; x <= 1; x++)
			{
				for (int z = -1; z <= 1; z++)
				{
					Vector2Int neighborCoord = chunkXZ + new Vector2Int(x, z);

					if (!WorldManager.s_ActiveChunks.ContainsKey(neighborCoord) && !pendingChunks.Contains(neighborCoord))
					{
						TerrainChunk savedChunk = WorldManager.OnRequestChunkData?.Invoke(neighborCoord);
						if (savedChunk != null)
						{
							WorldManager.s_ActiveChunks.TryAdd(neighborCoord, (savedChunk, null));
						}
						else if (OnGenerateBaseTerrain != null)
						{
							pendingChunks.Add(neighborCoord);
							yield return StartCoroutine(OnGenerateBaseTerrain.Invoke(neighborCoord));
							pendingChunks.Remove(neighborCoord);
						}
					}
				}
			}

			yield return new WaitUntil(() => CheckNeighborhoodReady(chunkXZ));

			// Decorate
			TerrainChunk targetChunk = WorldManager.s_ActiveChunks[chunkXZ].chunkData;
			if (targetChunk != null && targetChunk.ChunkGenerationState == TerrainChunk.EChunkGenerationState.BaseTerrain)
			{
				OnDecorateChunk?.Invoke(targetChunk);
				targetChunk.SetGenerationState(TerrainChunk.EChunkGenerationState.Decorated);
			}

			// Create physical GameObject container
			string chunkName = $"Chunk {chunkXZ.x}, {chunkXZ.y}";
			GameObject chunkObject = new GameObject(chunkName, typeof(MeshRenderer), typeof(MeshFilter), typeof(MeshCollider));
			chunkObject.transform.position = new Vector3(chunkXZ.x * WorldManager.s_ChunkSize.x, 0f, chunkXZ.y * WorldManager.s_ChunkSize.z);
			chunkObject.isStatic = true;

			if (WorldManager.s_ActiveChunks.ContainsKey(chunkXZ))
			{
				WorldManager.s_ActiveChunks[chunkXZ] = (targetChunk, chunkObject);
			}
			else
			{
				Destroy(chunkObject);
				requestedChunks.Remove(chunkXZ);
				yield break;
			}

			if (targetChunk != null)
			{
				targetChunk.OnChunkUpdate += onChunkUpdateCallback;
			}

			// Notify cardinal neighbors
			for (int i = 0; i < 4; i++)
			{
				Vector2Int neighbourToUpdate = chunkXZ + DirectionsUtility.CardinalDirections2D[i];
				if (WorldManager.s_ActiveChunks.TryGetValue(neighbourToUpdate, out var neighbor))
				{
					neighbor.chunkData?.UpdateChunk();
				}
			}

			// Generate Mesh via listener
			Mesh chunkMesh = null;
			if (OnGenerateMesh != null)
			{
				yield return StartCoroutine(OnGenerateMesh.Invoke(targetChunk, (mesh) => chunkMesh = mesh));
			}

			// Apply Mesh & Features
			if (WorldManager.s_ActiveChunks.ContainsKey(chunkXZ) && WorldManager.s_ActiveChunks[chunkXZ].gameObject != null)
			{
				if (chunkMesh != null)
				{
					chunkObject.GetComponent<MeshFilter>().mesh = chunkMesh;
					chunkObject.GetComponent<MeshCollider>().sharedMesh = chunkMesh;
				}

				OnSpawnFeatures?.Invoke(targetChunk, chunkObject.transform);
			}

			requestedChunks.Remove(chunkXZ);
		}

		public void RebuildChunkMesh(Vector2Int chunkXZ, TerrainChunk chunkData, GameObject chunkObject)
		{
			OnRebuildMeshRequested?.Invoke(chunkXZ, chunkData, chunkObject);
		}

		private bool CheckNeighborhoodReady(Vector2Int centerCoord)
		{
			for (int x = -1; x <= 1; x++)
			{
				for (int z = -1; z <= 1; z++)
				{
					Vector2Int neighbor = centerCoord + new Vector2Int(x, z);
					if (!WorldManager.s_ActiveChunks.ContainsKey(neighbor))
						return false;
				}
			}
			return true;
		}
	}
}
