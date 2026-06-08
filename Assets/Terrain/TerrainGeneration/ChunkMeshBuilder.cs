using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

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
		public Action<Mesh> OnComplete;
	}

	// --- System ---
	private WorldBuilder m_worldBuilder;
	private TileIndex m_tileIndex;
	private Dictionary<int, Color32> m_tileColorDict = new Dictionary<int, Color32>();

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
				m_tileColorDict[i] = voxelData.TileVertexColour;
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

	private void ProcessTile(Vector2Int chunkXZ, int[,,] chunkTiles, Vector3Int localPos, List<Vector3> vertices, List<int> triangles, List<Color32> colors)
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
				if(neighboursTileID >= 0 && 
					neighboursTileID < m_tileIndex.Tiles.Length)
				{
					// Ignore feature tiles
					if (m_tileIndex.Tiles[neighboursTileID] is FeatureTileData)
						continue;

					configBitmask |= 1 << i;
				}
			}
		}

		// Skip air or blocked cubes
		if (configBitmask == 0 || configBitmask == 255)
			return;

		// Ensure only voxel tiles are meshed
		Color32 tileColor = new Color32(255, 255, 255, 255);
		if (tileID >= 0 && tileID < m_tileIndex.Tiles.Length)
		{
			tileColor = m_tileColorDict[tileID];
		}
		else
		{
			// Air tiles with gemetry moving through them sample the first valid neighbours colour
			for (int i = 0; i < 8; i++)
			{
				if (TerrainChunkUtilities.IsNeighborTileSolid(chunkXZ, chunkTiles, localPos, MarchingTable.Corners[i], out int neighborID))
				{
					if (neighborID >= 0 && neighborID < m_tileIndex.Tiles.Length && m_tileColorDict.ContainsKey(neighborID))
					{
						tileColor = m_tileColorDict[neighborID];
						break;
					}
				}
			}
		}

		int edgeIndex = 0;
		for (int t = 0; t < 5; t++)
		{
			int idx1 = MarchingTable.Triangles[configBitmask, edgeIndex];
			if (idx1 == -1)
				break; // Correctly breaks out of triangle loops

			int idx2 = MarchingTable.Triangles[configBitmask, edgeIndex + 1];
			int idx3 = MarchingTable.Triangles[configBitmask, edgeIndex + 2];

			// Reverse wind the vertices
			AddVertexAndIndex(chunkXZ, chunkTiles, idx1, localPos, vertices, triangles, colors, tileColor);
			AddVertexAndIndex(chunkXZ, chunkTiles, idx3, localPos, vertices, triangles, colors, tileColor);
			AddVertexAndIndex(chunkXZ, chunkTiles, idx2, localPos, vertices, triangles, colors, tileColor);

			edgeIndex += 3;
		}
	}

	private Vector3 InterpolateVertex(Vector3 p1, Vector3 p2, float val1, float val2, float isoLevel = 0.5f)
	{
		// If the density exactly matches the isolevel, snap to the corner
		if (Mathf.Abs(isoLevel - val1) < 0.00001f)
			return p1;
		if (Mathf.Abs(isoLevel - val2) < 0.00001f)
			return p2;

		// If both corners have the exact same density, default to p1 (edge case)
		if (Mathf.Abs(val1 - val2) < 0.00001f)
			return p1;

		// Calculate the interpolation weight
		float t = (isoLevel - val1) / (val2 - val1);
		return Vector3.Lerp(p1, p2, t);
	}

	private float GetFakedDensity(Vector2Int chunkXZ, int[,,] chunkTiles, Vector3Int localPos)
	{
		Vector3Int chunkSize = WorldBuilder.s_ChunkSize;
		int solidCount = 0;
		int totalChecked = 0;

		// Sample the immediate 3x3x3 neighborhood
		for (int x = -1; x <= 1; x++)
		{
			for (int y = -1; y <= 1; y++)
			{
				for (int z = -1; z <= 1; z++)
				{
					Vector3Int offset = new Vector3Int(x, y, z);
					Vector3Int targetPos = localPos + offset;

					if (targetPos.y < 0 || targetPos.y >= chunkSize.y)
					{
						targetPos.y = Mathf.Clamp(targetPos.y, 0, chunkSize.y - 1);
					}

					// Using your existing neighbor check
					if (TerrainChunkUtilities.IsNeighborTileSolid(chunkXZ, chunkTiles, localPos, offset, out int tileId))
					{
						if (tileId >= 0 && 
							tileId < m_tileIndex.Tiles.Length && 
							!(m_tileIndex.Tiles[tileId] is FeatureTileData))
						{
							solidCount++;
						}
					}
					totalChecked++;
				}
			}
		}

		// Returns a smooth float between 0.0 (pure air) and 1.0 (deep underground)
		return (float)solidCount / totalChecked;
	}

	private void AddVertexAndIndex(Vector2Int chunkXZ, int[,,] chunkTiles, int edgeIdx, Vector3Int cubePos, List<Vector3> vertices, List<int> triangles, List<Color32> colors, Color32 color)
	{
		// The relative offset of the corners that make up this edge
		Vector3 edgeStartLocal = MarchingTable.Edges[edgeIdx, 0];
		Vector3 edgeEndLocal = MarchingTable.Edges[edgeIdx, 1];

		// The actual world/chunk grid coordinates of those corners
		Vector3Int startGridPos = cubePos + Vector3Int.RoundToInt(edgeStartLocal);
		Vector3Int endGridPos = cubePos + Vector3Int.RoundToInt(edgeEndLocal);

		// Calculate the smooth density values for both corners
		float densityA = GetFakedDensity(chunkXZ, chunkTiles, startGridPos);
		float densityB = GetFakedDensity(chunkXZ, chunkTiles, endGridPos);

		// The exact 3D spatial position of the corners
		Vector3 p1 = cubePos + edgeStartLocal;
		Vector3 p2 = cubePos + edgeEndLocal;

		// Find the interpolated intersection point
		Vector3 vertexPos = InterpolateVertex(p1, p2, densityA, densityB, 0.5f);
		vertices.Add(vertexPos);
		triangles.Add(vertices.Count - 1);
		colors.Add(color);
	}

	public IEnumerator GenerateMesh(Vector2Int chunkXZ, int[,,] chunkTiles, Action<Mesh> callback)
	{
		Task waitTask = m_meshingSemaphore.WaitAsync();
		yield return new WaitUntil(() => waitTask.IsCompleted);

		Vector3Int chunkSize = WorldBuilder.s_ChunkSize;

		// Worker thread
		Task<(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles, List<Color32> colors)> t =
		Task.Run(() =>
		{
			var vertices = new List<Vector3>();
			var normals = new List<Vector3>();
			var uvs = new List<Vector2>();
			var triangles = new List<int>();
			var colors = new List<Color32>();

			// Get cube vertices by checking all eight neighbours
			for (int x = 0; x < chunkSize.x; x++)
			{
				for (int y = 0; y < chunkSize.y; y++)
				{
					for (int z = 0; z < chunkSize.z; z++)
					{
						ProcessTile(chunkXZ, chunkTiles, new Vector3Int(x,y,z), vertices, triangles, colors);
					}
				}
			}

			for (int i = 0; i < vertices.Count; i++)
			{
				normals.Add(vertices[i]);
				uvs.Add(new Vector2(vertices[i].x, vertices[i].z)); // Planar UV
			}

			return (vertices, normals, uvs, triangles, colors);
		});

		yield return new WaitUntil(() => t.IsCompleted);
		m_meshingSemaphore.Release();

		var (newVertices, newNormals, newUVs, newTriangles, newColors) = t.Result;

		// Set mesh data
		Mesh finalMesh = new Mesh();
		finalMesh.SetVertices(newVertices);
		finalMesh.SetNormals(newNormals);
		finalMesh.SetUVs(0, newUVs);
		finalMesh.SetColors(newColors);
		finalMesh.SetTriangles(newTriangles, 0);

		finalMesh.RecalculateNormals();
		finalMesh.RecalculateTangents();
		finalMesh.RecalculateBounds();
		callback(finalMesh);
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