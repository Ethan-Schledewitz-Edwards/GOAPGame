using System;
using Unity.AI.Navigation;
using UnityEngine;

[RequireComponent(typeof(TerrainLoader))]
public class WorldNavMesher : MonoBehaviour
{
	[SerializeField] private NavMeshSurface m_navMeshSurface;

	// System
	private TerrainLoader m_terrainLoader;

	private void Awake()
	{
		m_terrainLoader = GetComponent<TerrainLoader>();

		m_terrainLoader.OnTerrainFinishedLoading += GenerateNavmesh;
	}

	private void GenerateNavmesh()
	{
		m_navMeshSurface.BuildNavMesh();
	}
}
