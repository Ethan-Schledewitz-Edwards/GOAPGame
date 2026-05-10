using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Loads chunks based on the player's current position.
/// </summary>
public class TerrainLoader : MonoBehaviour
{
	[SerializeField] private Transform m_player;
	[SerializeField] private int m_renderDist = 4;

	private WorldBuilder m_worldBuilder;
	private List<Vector2Int> m_chunksToUnload = new List<Vector2Int>();

	private bool m_isDirty = false;
	private bool m_isProcessing = false;

	public event Action OnTerrainFinishedLoading;

	private void Awake()
	{
		m_worldBuilder = GetComponent<WorldBuilder>();
	}

	private void Start()
	{
		LoadBatchOfChunks();
	}

	private void Update()
	{
		LoadBatchOfChunks();
	}

	private void LoadBatchOfChunks()
	{
		// Ignore requests for new batches if processing
		if (m_isProcessing) 
			return;

		int playerX = (int)(m_player.position.x / WorldBuilder.s_ChunkSize.x);
		int playerZ = (int)(m_player.position.z / WorldBuilder.s_ChunkSize.z);

		m_chunksToUnload.Clear();
		foreach (KeyValuePair<Vector2Int, (TerrainChunk, GameObject)> activeChunk in WorldBuilder.s_ActiveChunks)
		{
			m_chunksToUnload.Add(activeChunk.Key);
		}

		List<Vector2Int> chunksToLoad = new List<Vector2Int>();

		// Fetch chunks in a spiral
		int i = 0, j = 0;
		int di = 1, dj = 0;
		int segmentLength = 1;
		int segmentPassed = 0;
		int maxChunks = (2 * m_renderDist + 1) * (2 * m_renderDist + 1);

		for (int k = 0; k < maxChunks; ++k)
		{
			Vector2Int chunkCoord = new Vector2Int(playerX + i, playerZ + j);
			bool isChunkLoaded = WorldBuilder.s_ActiveChunks.ContainsKey(chunkCoord);

			if (!isChunkLoaded)
				chunksToLoad.Add(chunkCoord);
			else
				m_chunksToUnload.Remove(chunkCoord);

			i += di; j += dj; segmentPassed++;
			if (segmentPassed == segmentLength)
			{
				segmentPassed = 0;
				int temp = di; di = -dj; dj = temp;
				if (dj == 0) segmentLength++;
			}
		}

		if (chunksToLoad.Count > 0)
		{
			m_isDirty = true;
			StartCoroutine(LoadProcess(chunksToLoad));
		}
		else if (m_isDirty)
		{
			// If zero chunks are left to load, finish.
			m_isDirty = false;
			OnTerrainFinishedLoading?.Invoke();
			Debug.Log("Terrain Finished Loading!");
		}

		foreach (Vector2Int chunkXZ in m_chunksToUnload)
		{
			m_worldBuilder.RemoveActiveChunk(chunkXZ);
		}
	}

	private IEnumerator LoadProcess(List<Vector2Int> chunks)
	{
		m_isProcessing = true;

		// Wait for the WorldBuilder to finish its batch
		yield return StartCoroutine(m_worldBuilder.CreateActiveChunksBatch(chunks));

		m_isProcessing = false;
	}
}
