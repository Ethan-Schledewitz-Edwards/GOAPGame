using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TerrainGeneration.WorldProperties;

namespace TerrainGeneration.Core
{
	public static class ChunkUtility
	{
		// Move this to a chunk utilites class and rename this one to tile utils

		public static Vector2Int[] GetChunkCoordinatesInRadius(Vector3 worldPosition, int checkRadius)
		{
			int playerX = (int)(worldPosition.x / WorldPropertyUtility.s_ChunkSize.x);
			int playerZ = (int)(worldPosition.z / WorldPropertyUtility.s_ChunkSize.z);

			List<Vector2Int> surroundingChunks = new List<Vector2Int>();

			// Fetch chunks in a spiral
			int i = 0, j = 0;
			int di = 1, dj = 0;
			int segmentLength = 1;
			int segmentPassed = 0;
			int maxChunks = (2 * checkRadius + 1) * (2 * checkRadius + 1);

			for (int k = 0; k < maxChunks; ++k)
			{
				Vector2Int chunkCoordinate = new Vector2Int(playerX + i, playerZ + j);
				surroundingChunks.Add(chunkCoordinate);

				i += di; j += dj; segmentPassed++;
				if (segmentPassed == segmentLength)
				{
					segmentPassed = 0;
					int temp = di; di = -dj; dj = temp;
					if (dj == 0) segmentLength++;
				}
			}

			return surroundingChunks.ToArray();
		}
	}
}