using System;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Interaction.ActorInteractables.Structures
{
	[Serializable]
	public enum LinkNavMeshMode
	{
		Obstacle,
		Traversable
	}

	[RequireComponent(typeof(BoxCollider))]
	public class LinkStructureSegmentGenerator : MonoBehaviour
	{
		[Header("Settings & Visuals")]
		[SerializeField] private Material m_segmentMaterial;
		[SerializeField] private Mesh m_segmentMesh;
		[SerializeField] private float m_segmentSpacing = 1.5f;

		[Header("NavMesh")]
		[SerializeField] private LinkNavMeshMode m_navMeshMode = LinkNavMeshMode.Obstacle;

		private BoxCollider m_boxCollider;
		private NavMeshObstacle m_navMeshObstacle;

		private Vector3 m_linkStartPosition;
		private Vector3 m_linkEndPosition;

		private void Awake()
		{
			m_boxCollider = GetComponent<BoxCollider>();
		}

		public void SetLinkStartPosition(Vector3 position)
		{
			m_linkStartPosition = position;
		}

		public void SetLinkEndPosition(Vector3 position)
		{
			m_linkEndPosition = position;
		}

		public void Initialize(Vector3 startPosition, Vector3 endPosition, Mesh mesh, float spacing)
		{
			m_linkStartPosition = startPosition;
			m_linkEndPosition = endPosition;

			if (mesh != null)
				m_segmentMesh = mesh;

			if (spacing > 0f)
				m_segmentSpacing = spacing;
		}

		/// <summary>
		/// Generates and places mesh segments between the start and end positions, 
		/// then configures the BoxCollider to match the total span.
		/// </summary>
		public void GenerateLink()
		{
			if (m_segmentMesh == null)
			{
				Debug.LogWarning("Tried to generate link structure segments, but no segment mesh is assigned.", this);
				return;
			}

			Vector3 direction = m_linkEndPosition - m_linkStartPosition;
			float totalDistance = direction.magnitude;

			if (totalDistance <= 0f)
				return;

			direction.Normalize();
			Quaternion pathRotation = Quaternion.LookRotation(direction, Vector3.up);

			int segmentCount = Mathf.FloorToInt(totalDistance / m_segmentSpacing);

			// Instantiate segments along the path
			for (int j = 1; j <= segmentCount; j++)
			{
				Vector3 spawnPosition = m_linkStartPosition + (direction * (j * m_segmentSpacing));

				GameObject segmentObject = new GameObject($"Segment_{j}");
				segmentObject.transform.SetParent(transform, true);
				segmentObject.transform.position = spawnPosition;
				segmentObject.transform.rotation = pathRotation;

				MeshFilter filter = segmentObject.AddComponent<MeshFilter>();
				MeshRenderer renderer = segmentObject.AddComponent<MeshRenderer>();

				filter.mesh = m_segmentMesh;
				renderer.sharedMaterial = m_segmentMaterial;
			}

			Vector3 finalSegmentPosition = m_linkStartPosition + (transform.forward * (segmentCount * m_segmentSpacing));
			Vector3 linkWorldCenter = (m_linkStartPosition + finalSegmentPosition) * 0.5f;

			// Configure BoxCollider center and size
			if (m_boxCollider != null)
			{
				// Calculate center in local space
				m_boxCollider.center = transform.InverseTransformPoint(linkWorldCenter);

				// Scale the box collider along the Z-axis
				Vector3 colliderSize = m_boxCollider.size;
				colliderSize.z = segmentCount * m_segmentSpacing;
				m_boxCollider.size = colliderSize;
			}

			// Configure NavMesh behavior based on whether it's a Wall (Obstacle) or Bridge (Traversable)
			if (m_navMeshMode == LinkNavMeshMode.Obstacle)
			{
				if (m_navMeshObstacle == null)
				{
					m_navMeshObstacle = gameObject.AddComponent<NavMeshObstacle>();
				}

				m_navMeshObstacle.enabled = true;
				m_navMeshObstacle.carving = true;
				m_navMeshObstacle.center = transform.InverseTransformPoint(linkWorldCenter);

				Vector3 obstacleSize = m_navMeshObstacle.size;
				obstacleSize.z = segmentCount * m_segmentSpacing;
				m_navMeshObstacle.size = obstacleSize;
			}
			else if (m_navMeshMode == LinkNavMeshMode.Traversable)
			{
				if (m_navMeshObstacle != null)
					m_navMeshObstacle.enabled = false;

				UpdateNavMeshForBridge();
			}
		}

		private void UpdateNavMeshForBridge()
		{
			// We gunna have to use A*

			//NavMeshSurface surface = FindAnyObjectByType<NavMeshSurface>();
			//if (surface != null)
			//{
			//	surface.UpdateNavMesh(surface.navMeshData);
			//}
			//else
			//{
			//	Debug.LogWarning("Bridge structure generated, but no NavMeshSurface was found in the scene to update.", this);
			//}
		}
	}
}
