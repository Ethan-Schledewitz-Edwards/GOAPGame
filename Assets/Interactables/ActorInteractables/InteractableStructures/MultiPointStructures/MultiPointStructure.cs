using System.Collections.Generic;
using UnityEngine;

namespace Interaction.ActorInteractables.Structures
{
    public class MultiPointStructure : MonoBehaviour
    {
		[SerializeField] private Transform m_segmentObject;

		public void GenerateMultiPointStructureLinks(List<Vector3> points, float spacing)
		{
			if (m_segmentObject == null ||
				points.Count < 2)
				return;

			// Loop through each link
			for (int i = 0; i < points.Count - 1; i++)
			{
				Vector3 start = points[i];
				Vector3 end = points[i + 1];

				Vector3 direction = end - start;
				float totalDistance = direction.magnitude;
				if (totalDistance <= 0.001f)
					continue;
				direction.Normalize();

				int segmentCount = Mathf.FloorToInt(totalDistance / spacing);
				Quaternion pathRotation = Quaternion.LookRotation(direction, Vector3.up);

				// Place each segment between this for this link
				for (int j = 0; j <= segmentCount; j++)
				{
					// Skip the first segment of the first link since it overlaps with the base segment
					if (i == 0 && j == 0)
						continue;

					Vector3 spawnPosition = start + (direction * (j * spacing));

					GameObject segmentObject = new GameObject($"Segment_{i}_{j}");
					segmentObject.transform.SetParent(transform, true);
					segmentObject.transform.position = spawnPosition;
					segmentObject.transform.rotation = pathRotation;
					segmentObject.SetActive(true);
				}
			}
		}
	}
}
