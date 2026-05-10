using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Interprets the block data stored in a given chunk coordinate position.
/// Using this data, a mesh is built and features are spawned. 
/// The final mesh data is passed to the world generator.
/// </summary>
public class ChunkMeshBuilder
{
	// Sub-classes
	public class GeneratingChunkMesh
	{
		public int[,,] TileData;
		public Vector2Int ChunkXZ;
		public Action<Mesh, Material[]> OnComplete;
	}

	// --- System ---
	private WorldBuilder m_worldBuilder;
	private TileIndex m_tileIndex;
	private Dictionary<int, Material> m_tileMaterialDict = new Dictionary<int, Material>();

	// Multithreading
	private Queue<GeneratingChunkMesh> m_meshQueue = new Queue<GeneratingChunkMesh>();
	private SemaphoreSlim m_meshingSemaphore;
	private bool m_isMeshingEnabled = true;

	#region Constructor

	public ChunkMeshBuilder(WorldBuilder worldBuilder, TileIndex tileIndex)
	{
		this.m_tileIndex = tileIndex;
		this.m_worldBuilder = worldBuilder;

		for (int i = 0; i < m_tileIndex.Tiles.Length; i++)
		{
			if (m_tileIndex.Tiles[i] is VoxelTileData voxelData)
				m_tileMaterialDict[i] = voxelData.TileMaterial;
		}

		m_meshingSemaphore = new SemaphoreSlim(4);
		worldBuilder.StartCoroutine(MeshFromQueue());
	}

	#endregion

	#region Mesh Generation

	public void QueueDataToGenerate(GeneratingChunkMesh data)
	{
		m_meshQueue.Enqueue(data);
	}

	private void ProcessTile(Vector2Int chunkXZ, int[,,] chunkTiles, Vector3Int localPos, List<Vector3> vertices, Dictionary<int, List<int>> trianglesByMaterial)
	{
		Vector3Int ChunkSize = WorldBuilder.s_ChunkSize;
		int configBitmask = 0;

		int tileID = chunkTiles[localPos.x, localPos.y, localPos.z];

		// Determine the configuration index for this cube
		for (int i = 0; i < 8; i++)
		{
			Vector3Int cornerPos = localPos + MarchingTable.Corners[i];
			if (TerrainChunkUtilities.IsNeighborTileSolid(chunkXZ, chunkTiles, localPos, MarchingTable.Corners[i], out int neighboursTileID))
			{
				if(neighboursTileID > 0)
				{
					// Ignore feature tiles
					if (m_tileIndex.Tiles[neighboursTileID - 1] is FeatureTileData)
						continue;

					configBitmask |= 1 << i;
				}
			}
		}

		// Skip air or blocked cubes
		if (configBitmask == 0 || configBitmask == 255)
			return;

		// Ensure only voxel tiles are meshed
		if (m_tileIndex.Tiles[tileID] is VoxelTileData voxelData)
		{
			if (!trianglesByMaterial.ContainsKey(tileID))
				trianglesByMaterial[tileID] = new List<int>();

			// Generate triangles
			int edgeIndex = 0;
			for (int t = 0; t < 5; t++)
			{
				int idx1 = MarchingTable.Triangles[configBitmask, edgeIndex];

				if (idx1 == -1)
					return;

				int idx2 = MarchingTable.Triangles[configBitmask, edgeIndex + 1];
				int idx3 = MarchingTable.Triangles[configBitmask, edgeIndex + 2];

				// Reverse wind the vertices
				AddVertexAndIndex(idx1, localPos, vertices, trianglesByMaterial[tileID]);
				AddVertexAndIndex(idx3, localPos, vertices, trianglesByMaterial[tileID]);
				AddVertexAndIndex(idx2, localPos, vertices, trianglesByMaterial[tileID]);

				edgeIndex += 3;
			}
		}
	}

	private void AddVertexAndIndex(int edgeIdx, Vector3Int cubePos, List<Vector3> vertices, List<int> triangleIndices)
	{
		Vector3 edgeStart = cubePos + MarchingTable.Edges[edgeIdx, 0];
		Vector3 edgeEnd = cubePos + MarchingTable.Edges[edgeIdx, 1];
		Vector3 vertexPos = (edgeStart + edgeEnd) / 2f;

		vertices.Add(vertexPos);
		triangleIndices.Add(vertices.Count - 1);
	}

	public IEnumerator GenerateMesh(Vector2Int chunkXZ, int[,,] chunkTiles, Action<Mesh, Material[]> callback)
	{
		Task waitTask = m_meshingSemaphore.WaitAsync();
		yield return new WaitUntil(() => waitTask.IsCompleted);

		Vector3Int chunkSize = WorldBuilder.s_ChunkSize;

		// Worker thread
		Task<(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, Dictionary<int, List<int>> trianglesByMaterial)> t =
		Task.Run(() =>
		{
			var vertices = new List<Vector3>();
			var normals = new List<Vector3>();
			var uvs = new List<Vector2>();
			var trianglesByMaterial = new Dictionary<int, List<int>>();

			// Get cube vertices by checking all eight neighbours
			for (int x = 0; x < chunkSize.x; x++)
			{
				for (int y = 0; y < chunkSize.y; y++)
				{
					for (int z = 0; z < chunkSize.z; z++)
					{
						ProcessTile(chunkXZ, chunkTiles, new Vector3Int(x,y,z), vertices, trianglesByMaterial);
					}
				}
			}

			for (int i = 0; i < vertices.Count; i++)
			{
				normals.Add(vertices[i]);
				uvs.Add(new Vector2(vertices[i].x, vertices[i].z)); // Planar UV
			}

			return (vertices, normals, uvs, trianglesByMaterial);
		});

		yield return new WaitUntil(() => t.IsCompleted);
		m_meshingSemaphore.Release();

		var (newVertices, newNormals, newUVs, newTrianglesByMaterial) = t.Result;

		// Set mesh data
		Mesh finalMesh = new Mesh();
		finalMesh.SetVertices(newVertices);
		finalMesh.SetNormals(newNormals);
		finalMesh.SetUVs(0, newUVs);
		finalMesh.subMeshCount = newTrianglesByMaterial.Count;

		Material[] finalMaterials = new Material[finalMesh.subMeshCount];
		int subMeshIndex = 0;

		foreach (var pair in newTrianglesByMaterial)
		{
			finalMesh.SetTriangles(pair.Value.ToArray(), subMeshIndex);

			// Ensure the material exists in the dictionary
			if (m_tileMaterialDict.TryGetValue(pair.Key, out Material mat))
				finalMaterials[subMeshIndex] = mat;

			subMeshIndex++;
		}

		finalMesh.RecalculateNormals();
		finalMesh.RecalculateBounds();
		callback(finalMesh, finalMaterials);
	}

	/// <summary>
	/// Meshes all of the chunks present in the queue.
	/// </summary>
	public IEnumerator MeshFromQueue()
	{
		while (m_isMeshingEnabled)
		{
			if (m_meshQueue.Count > 0)
			{
				GeneratingChunkMesh chunk = m_meshQueue.Dequeue();
				yield return m_worldBuilder.StartCoroutine(GenerateMesh(chunk.ChunkXZ, chunk.TileData, chunk.OnComplete));
			}
			yield return null;
		}
	}

	#endregion
}