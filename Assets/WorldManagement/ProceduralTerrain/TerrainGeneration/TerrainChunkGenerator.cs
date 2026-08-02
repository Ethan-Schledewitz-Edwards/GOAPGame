using System;
using System.Collections;
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

			m_chunkManager.OnGenerateBaseTerrain += GenerateBaseTerrain;
			m_chunkManager.OnDecorateChunk += DecorateChunk;
			m_chunkManager.OnGenerateMesh += GenerateMesh;
			m_chunkManager.OnSpawnFeatures += SpawnFeatures;
			m_chunkManager.OnRebuildMeshRequested += RebuildMesh;
		}

		private void OnDisable()
		{
			m_chunkManager.OnGenerateBaseTerrain -= GenerateBaseTerrain;
			m_chunkManager.OnDecorateChunk -= DecorateChunk;
			m_chunkManager.OnGenerateMesh -= GenerateMesh;
			m_chunkManager.OnSpawnFeatures -= SpawnFeatures;
			m_chunkManager.OnRebuildMeshRequested -= RebuildMesh;
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

		private void DecorateChunk(TerrainChunk chunk)
		{
			m_chunkBuilder.DecorateChunk(chunk);
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
	}
}
