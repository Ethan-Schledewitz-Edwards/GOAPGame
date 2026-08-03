using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WorldManagement.Core;
using WorldManagement.Tiles;

namespace WorldManagement.TerrainGeneration
{
	[RequireComponent(typeof(TerrainChunkManager))]
    public class TerrainChunkGenerator : MonoBehaviour
	{
		[Header("Procedural References")]
		[SerializeField] private Material m_terrainMaterial;
		[SerializeField] private TileIndex m_tileIndex;
		[SerializeField] private BiomeIndex m_biomeIndex;

		private ChunkDataBuilder m_chunkBuilder;
		private ChunkMeshBuilder m_chunkMesher;
		private TerrainChunkManager m_chunkManager;

		private void Awake()
		{
			m_chunkManager = GetComponent<TerrainChunkManager>();
			m_chunkBuilder = new ChunkDataBuilder(this, m_biomeIndex);
			m_chunkMesher = new ChunkMeshBuilder(this);
		}

		private void OnEnable()
		{
			if (m_chunkManager.BuilderMethod != TerrainChunkManager.EChunkBuilderMethod.Procedural)
				return;

			m_chunkManager.OnProcessChunkSpawn += GenerateProceduralChunk;
			m_chunkManager.OnProcessChunkRebuild += RebuildMesh;
		}

		private void OnDisable()
		{
			if (m_chunkManager.BuilderMethod != TerrainChunkManager.EChunkBuilderMethod.Procedural)
				return;

			m_chunkManager.OnProcessChunkSpawn -= GenerateProceduralChunk;
			m_chunkManager.OnProcessChunkRebuild -= RebuildMesh;
		}

		private IEnumerator GenerateProceduralChunk
			(
				Vector2Int chunkXZ,
				HashSet<Vector2Int> requestedChunks,
				HashSet<Vector2Int> pendingChunks,
				Action<Vector2Int> chunkUpdated,
				Action<TerrainChunk, GameObject> chunkGenerated
			)
		{
			requestedChunks.Add(chunkXZ);

			// Generate a 3x3 neighbour hood of chunks
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
						else
						{
							pendingChunks.Add(neighborCoord);
							yield return StartCoroutine(GenerateBaseTerrain(neighborCoord));
							pendingChunks.Remove(neighborCoord);
						}
					}
				}
			}

			yield return new WaitUntil(() => CheckNeighborhoodReady(chunkXZ));

			// Decorate the center chunk
			TerrainChunk targetChunk = WorldManager.s_ActiveChunks[chunkXZ].chunkData;
			if (targetChunk != null && targetChunk.ChunkGenerationState == TerrainChunk.EChunkGenerationState.BaseTerrain)
			{
				m_chunkBuilder.DecorateChunk(targetChunk);
				targetChunk.SetGenerationState(TerrainChunk.EChunkGenerationState.Decorated);
			}

			// Create the chunks GameObject
			string chunkName = $"Chunk({chunkXZ.x}, {chunkXZ.y})";
			GameObject chunkObject = new GameObject(chunkName, typeof(MeshRenderer), typeof(MeshFilter), typeof(MeshCollider));
			chunkObject.transform.position = new Vector3(chunkXZ.x * WorldManager.s_ChunkSize.x, 0f, chunkXZ.y * WorldManager.s_ChunkSize.z);
			chunkObject.isStatic = true;

			if (targetChunk != null)
			{
				targetChunk.OnChunkUpdate += chunkUpdated;
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

			// Generate the chunks mesh
			Mesh chunkMesh = null;
			yield return StartCoroutine(GenerateMesh(targetChunk, (mesh) => chunkMesh = mesh));

			// Apply Mesh
			if (chunkMesh != null)
			{
				chunkObject.GetComponent<MeshFilter>().mesh = chunkMesh;
				chunkObject.GetComponent<MeshCollider>().sharedMesh = chunkMesh;
			}

			SpawnFeatures(targetChunk, chunkObject.transform);

			// Pass the finished chunk back to the manager
			chunkGenerated?.Invoke(targetChunk, chunkObject);
			requestedChunks.Remove(chunkXZ);
		}

		private IEnumerator GenerateBaseTerrain(Vector2Int chunkXZ)
		{
			bool isComplete = false;
			m_chunkBuilder.QueueDataToGenerate(new ChunkDataBuilder.GeneratingChunk
			{
				ChunkXZ = chunkXZ,
				OnGenerationComplete = (tileData, biomeMap) =>
				{
					TerrainChunk newChunk = new TerrainChunk(chunkXZ, tileData, biomeMap);
					newChunk.SetGenerationState(TerrainChunk.EChunkGenerationState.BaseTerrain);
					WorldManager.s_ActiveChunks.TryAdd(chunkXZ, (newChunk, null));
					isComplete = true;
				}
			});

			yield return new WaitUntil(() => isComplete);
		}

		private IEnumerator GenerateMesh(TerrainChunk chunk, Action<Mesh> onComplete)
		{
			if (chunk == null) yield break;

			bool isComplete = false;
			m_chunkMesher.QueueDataToGenerate(new ChunkMeshBuilder.GeneratingChunkMesh
			{
				ChunkXZ = chunk.ChunkXZ,
				TileData = chunk.TileData,
				OnComplete = (mesh) =>
				{
					onComplete?.Invoke(mesh);
					isComplete = true;
				}
			});

			yield return new WaitUntil(() => isComplete);
		}

		private void SpawnFeatures(TerrainChunk chunk, Transform parent)
		{
			if (m_tileIndex == null || chunk == null) return;

			MeshRenderer renderer = parent.GetComponent<MeshRenderer>();
			if (renderer != null) renderer.material = m_terrainMaterial;

			Vector3Int chunkSize = WorldManager.s_ChunkSize;
			for (int x = 0; x < chunkSize.x; x++)
			{
				for (int z = 0; z < chunkSize.z; z++)
				{
					for (int y = 0; y < chunkSize.y; y++)
					{
						int tileID = chunk.TileData[x, y, z];

						if (tileID >= 0 && tileID < m_tileIndex.AssetsInIndex)
						{
							if (m_tileIndex.GetIndexedAsset(tileID) is FeatureTileData featureData && featureData.Prefab != null)
							{
								GameObject featureTile = Instantiate(featureData.Prefab, parent);
								featureTile.transform.localPosition = new Vector3(x, y, z);
								featureTile.transform.localRotation = Quaternion.identity;
							}
						}
					}
				}
			}
		}

		private void RebuildMesh(Vector2Int chunkXZ, TerrainChunk chunkData, GameObject chunkObject)
		{
			MeshFilter meshFilter = chunkObject.GetComponent<MeshFilter>();
			MeshCollider meshCollider = chunkObject.GetComponent<MeshCollider>();
			MeshRenderer meshRenderer = chunkObject.GetComponent<MeshRenderer>();

			StartCoroutine(m_chunkMesher.GenerateMesh(chunkXZ, chunkData.TileData, (mesh) =>
			{
				if (mesh != null && mesh.vertexCount > 0)
				{
					meshFilter.sharedMesh = mesh;
					meshCollider.sharedMesh = mesh;
					meshRenderer.material = m_terrainMaterial;
				}
				else
				{
					meshFilter.sharedMesh = null;
					meshCollider.sharedMesh = null;
				}
			}));
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
