using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class WorldBuilder : MonoBehaviour
{
	public static WorldBuilder Instance { get; private set; }

	public static readonly Vector3Int s_ChunkSize = new Vector3Int(16, 32, 16);

	[Header("Data")]
	public static BiomeIndex BiomeIndex => Instance.m_biomeIndex;
	[SerializeField] private BiomeIndex m_biomeIndex;

	public static TileIndex TileIndex => Instance.m_tileIndex;
	[SerializeField] private TileIndex m_tileIndex;

	[Header("Components")]
	private ChunkDataBuilder m_chunkBuilder;
	private ChunkBuilder m_chunkMesher;

	[Header("Seed")]
	public readonly static int s_Seed  = 64;

	[Header("System")]
	public static readonly Dictionary<Vector2Int, TerrainChunk> s_WorldData = new Dictionary<Vector2Int, TerrainChunk>();

	public static Dictionary<Vector2Int, (TerrainChunk chunkData, GameObject gameObject)> s_ActiveChunks = 
		new Dictionary<Vector2Int, (TerrainChunk chunkData, GameObject gameObject)>();

	private static readonly HashSet<Vector2Int> s_requestedChunks = new HashSet<Vector2Int>();

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;

		m_chunkBuilder = new ChunkDataBuilder(this, m_biomeIndex);
		m_chunkMesher = new ChunkBuilder(this, m_tileIndex);
	}

	/// <summary>
	/// Places a chunk GameObject in world space based on the data assigned to the chunks coordinates.
	/// </summary>
	public IEnumerator CreateActiveChunk(Vector2Int chunkXZ)
	{
		if (s_requestedChunks.Contains(chunkXZ) || s_ActiveChunks.ContainsKey(chunkXZ))
			yield break;

		s_requestedChunks.Add(chunkXZ);

		// Get chunk data
		TerrainChunk terrainChunk = null;
		if (s_WorldData.ContainsKey(chunkXZ))
		{
			// Load the chunks data
			terrainChunk = s_WorldData[chunkXZ];
		}
		else
		{
			// Generate the new chunks data
			m_chunkBuilder.QueueDataToGenerate(new ChunkDataBuilder.GeneratingChunk
			{
				ChunkXZ = chunkXZ,
				OnGenerationComplete = tileData =>
				{
					terrainChunk = new(chunkXZ, tileData);
					s_WorldData.TryAdd(chunkXZ, terrainChunk);
				}
			});

			yield return new WaitUntil(() => terrainChunk != null);
		}

		// Create a physical chunk GameObject
		string chunkName = $"Chunk {chunkXZ.x}, {chunkXZ.y}";
		GameObject chunkObject = new GameObject(chunkName, new System.Type[]
		{
			typeof(MeshRenderer),
			typeof(MeshFilter),
			typeof(MeshCollider),
		});

		chunkObject.transform.position = new Vector3(chunkXZ.x * s_ChunkSize.x, 0f, chunkXZ.y * s_ChunkSize.z);
		chunkObject.isStatic = true;

		// If the data generated again, destroy the duplicate
		if (!s_ActiveChunks.TryAdd(chunkXZ, (terrainChunk, chunkObject)))
		{
			Destroy(chunkObject);
			s_requestedChunks.Remove(chunkXZ);
			yield break;
		}

		terrainChunk.OnChunkUpdate += OnChunkUpdate;  // Subscribe to chunk updates

		// Convert the chunk's data into a mesh
		Mesh chunkMesh = null;
		Material[] chunkMaterials = null;
		m_chunkMesher.QueueDataToGenerate(new ChunkBuilder.GeneratingChunkMesh
		{
			ChunkXZ = terrainChunk.ChunkXZ,
			TileData = terrainChunk.TileData,

			OnComplete = (mesh, materials) =>
			{
				chunkMesh = mesh;
				chunkMaterials = materials; // Store the materials
			}
		});
		yield return new WaitUntil(() => chunkMesh != null);

		// Apply the generated mesh to the gameobject if the player has not unloaded the chunk
		if(s_ActiveChunks.ContainsKey(chunkXZ))
		{
			// Visualize chunk with final mesh
			MeshRenderer meshRenderer = chunkObject.GetComponent<MeshRenderer>();
			meshRenderer.materials = chunkMaterials;

			MeshFilter meshFilter = chunkObject.GetComponent<MeshFilter>();
			meshFilter.mesh = chunkMesh;

			MeshCollider meshCollider = chunkObject.GetComponent<MeshCollider>();
			meshCollider.sharedMesh = meshFilter.mesh;

			// Tell neighbouring chunks about a the new active chunk
			for (int i = 0; i < 4; i++)
			{
				Vector2Int neighbourToUpdate = chunkXZ + TerrainChunkUtilities.CardinalDirections2D[i];
				if (s_WorldData.ContainsKey(chunkXZ + TerrainChunkUtilities.CardinalDirections2D[i]))
				{
					s_WorldData[neighbourToUpdate].UpdateChunk();
				}
			}
			
			// Spawn all feature tiles
			for (int x = 0; x < s_ChunkSize.x; x++)
			{
				for (int z = 0; z < s_ChunkSize.z; z++)
				{
					for (int y = 0; y < s_ChunkSize.y; y++)
					{
						int tileID = terrainChunk.TileData[x, y, z];

						if (tileID == 0) 
							continue;

						int tileIndex = tileID - 1; // Remap value for the tile data array

						if (m_tileIndex.Tiles[tileIndex] is FeatureTileData featureData)
						{
							GameObject featureTile = GameObject.Instantiate
								(
									featureData.Prefab,
									chunkObject.transform
								);

							featureTile.transform.localPosition = new Vector3(x, y, z);
							featureTile.transform.localRotation = Quaternion.identity;
						}
					}

					// Probably put a loop here spawning all entities saved in the chunks data too
				}
			}
		}

		if (s_requestedChunks.Contains(chunkXZ))
			s_requestedChunks.Remove(chunkXZ);
	}

	public IEnumerator CreateActiveChunksBatch(List<Vector2Int> chunkCoords)
	{
		foreach (var coord in chunkCoords)
		{
			yield return CreateActiveChunk(coord);
		}
	}

	public void RemoveActiveChunk(Vector2Int chunkXZ)
	{
		var value = s_ActiveChunks[chunkXZ];

		if (s_requestedChunks.Contains(chunkXZ))
			s_requestedChunks.Remove(chunkXZ);

		value.chunkData.OnChunkUpdate -= OnChunkUpdate;
		Destroy(value.gameObject);

		if(s_ActiveChunks.ContainsKey(chunkXZ))
			s_ActiveChunks.Remove(chunkXZ);
	}

	public void OnChunkUpdate(Vector2Int chunkXZ)
	{
		if (s_ActiveChunks.ContainsKey(chunkXZ) && !s_requestedChunks.Contains(chunkXZ))
		{
			GameObject chunk = s_ActiveChunks[chunkXZ].gameObject;
			MeshFilter meshFilter = chunk.GetComponent<MeshFilter>();
			MeshCollider meshCollider = chunk.GetComponent<MeshCollider>();
			MeshRenderer meshRenderer = chunk.GetComponent<MeshRenderer>();

			StartCoroutine(m_chunkMesher.GenerateMesh(chunkXZ, s_WorldData[chunkXZ].TileData, (mesh, materials) =>
			{
				meshFilter.mesh = mesh;
				meshCollider.sharedMesh = mesh;
				meshRenderer.sharedMaterials = materials;
			}));

			//m_navMeshSurface?.BuildNavMesh();
		}
	}
}
