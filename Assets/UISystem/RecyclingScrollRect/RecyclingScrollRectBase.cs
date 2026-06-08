using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UISystems.RecyclingScrollRect
{
	public abstract class RecyclingScrollRectBase<T> : MonoBehaviour
	{
		[field: SerializeField] public T[] Data { get; private set; }

		[Header("Cells")]
		[SerializeField] private GameObject m_cellPrefab;
		private float m_cellWidth = 128, m_cellHeight = 256;
		[SerializeField] private bool m_isGrid;
		[SerializeField] private int m_columns = 1;
		[SerializeField] private float m_minPoolCoverage = 1.5f; // Extra sizing allocation multiplier
		[SerializeField] private int m_minPoolSize = 10;

		[Header("Rect Properties")]
		[Tooltip("The padding multiplier outside the viewport where cells remain active before being recycled.")]
		[SerializeField] private float m_recyclingThreshold = 0.1f;
		[SerializeField] private RectTransform m_viewport, m_content;

		private List<RectTransform> m_cellRectPool = new List<RectTransform>();

		private Bounds m_recyclableViewBounds;
		private readonly Vector3[] m_corners = new Vector3[4];

		// Trackers for recycling loops
		private int m_currentItemCount;
		private int m_topMostCellIndex, m_bottomMostCellIndex;
		private int m_topMostCellColumn, m_bottomMostCellColumn;
		private bool m_isRecycling;

		protected virtual void Start()
		{
			InitializeSystem();
		}

		public void InitializeSystem()
		{
			if (Data == null || Data.Length == 0) return;

			SetTopAnchor(m_content);
			m_content.anchoredPosition = Vector2.zero;

			DefineBounds();
			InitializeCellPool();

			// Set content height based on total rows generated
			int noOfRows = Mathf.CeilToInt((float)m_cellRectPool.Count / m_columns);
			m_content.sizeDelta = new Vector2(m_content.sizeDelta.x, noOfRows * m_cellHeight);
			SetTopAnchor(m_content);
		}

		private void DefineBounds()
		{
			if (m_viewport == null)
			{
				Debug.LogError("Viewport RectTransform is missing. Cannot define recycling bounds.", this);
				return;
			}

			// Index order: [0]Bottom-Left, [1]Top-Left, [2]Top-Right, [3] Bottom-Right
			m_viewport.GetWorldCorners(m_corners);

			// Calculate the height of the visible viewport
			float viewportHeight = m_corners[2].y - m_corners[0].y;

			// Determine the extra padding to apply to the top and bottom
			float threshHold = m_recyclingThreshold * viewportHeight;

			// Expand the min/max vectors of our bounding box by the threshold
			Vector3 minBounds = new Vector3(m_corners[0].x, m_corners[0].y - threshHold, m_corners[0].z);
			Vector3 maxBounds = new Vector3(m_corners[2].x, m_corners[2].y + threshHold, m_corners[2].z);

			// Assign the new boundary box
			m_recyclableViewBounds.SetMinMax(minBounds, maxBounds);
		}

		private void InitializeCellPool()
		{
			// Clear previous instances
			foreach (var rect in m_cellRectPool)
			{
				if (rect != null) 
					Destroy(rect.gameObject);
			}
			m_cellRectPool.Clear();

			if (m_cellPrefab == null) 
				return;

			RectTransform prefabRect = m_cellPrefab.GetComponent<RectTransform>();

			if (m_isGrid) 
				SetTopLeftAnchor(prefabRect);
			else 
				SetTopAnchor(prefabRect);

			m_topMostCellColumn = 0; 
			m_bottomMostCellColumn = 0;

			float currentPoolCoverage = 0;
			int poolSize = 0;
			float posX = 0;
			float posY = 0;

			m_cellWidth = m_content.rect.width / m_columns;
			m_cellHeight = (prefabRect.sizeDelta.y / prefabRect.sizeDelta.x) * m_cellWidth;

			float requiredCoverage = m_minPoolCoverage * m_viewport.rect.height;
			int minCalculatedPoolSize = Mathf.Min(m_minPoolSize, Data.Length);

			while ((poolSize < minCalculatedPoolSize || currentPoolCoverage < requiredCoverage) && poolSize < Data.Length)
			{
				GameObject spawnedObj = Instantiate(m_cellPrefab);
				RectTransform itemRect = spawnedObj.GetComponent<RectTransform>();
				itemRect.name = $"Cell_{poolSize}";
				itemRect.sizeDelta = new Vector2(m_cellWidth, m_cellHeight);
				itemRect.SetParent(m_content, false);

				m_cellRectPool.Add(itemRect);

				if (m_isGrid)
				{
					posX = m_bottomMostCellColumn * m_cellWidth;
					itemRect.anchoredPosition = new Vector2(posX, posY);
					if (++m_bottomMostCellColumn >= m_columns)
					{
						m_bottomMostCellColumn = 0;
						posY -= m_cellHeight;
						currentPoolCoverage += m_cellHeight;
					}
				}
				else
				{
					itemRect.anchoredPosition = new Vector2(0, posY);
					posY -= m_cellHeight;
					currentPoolCoverage += m_cellHeight;
				}

				if (spawnedObj.TryGetComponent<IRecyclableCell<T>>(out var cellComp))
					cellComp.ConfigureCell(poolSize, Data);

				poolSize++;
			}

			if (m_isGrid)
			{
				m_bottomMostCellColumn = (m_bottomMostCellColumn - 1 + m_columns) % m_columns;
			}

			m_currentItemCount = m_cellRectPool.Count;
			m_topMostCellIndex = 0;
			m_bottomMostCellIndex = m_cellRectPool.Count - 1;

			if (m_cellPrefab.scene.IsValid()) m_cellPrefab.SetActive(false);
		}

		public Vector2 OnScrollValueChanged(Vector2 scrollDirection)
		{
			if (m_isRecycling || m_cellRectPool == null || m_cellRectPool.Count == 0) return Vector2.zero;

			DefineBounds();

			// Convert world limits for quick boundary assessments
			float bottomCellMaxY = m_cellRectPool[m_bottomMostCellIndex].position.y + (m_cellHeight / 2f);
			float topCellMinY = m_cellRectPool[m_topMostCellIndex].position.y - (m_cellHeight / 2f);

			if (scrollDirection.y > 0 && bottomCellMaxY > m_recyclableViewBounds.min.y)
			{
				return RecycleTopToBottom();
			}
			else if (scrollDirection.y < 0 && topCellMinY < m_recyclableViewBounds.max.y)
			{
				return RecycleBottomToTop();
			}

			return Vector2.zero;
		}

		private Vector2 RecycleTopToBottom()
		{
			m_isRecycling = true;
			int rowsShifted = 0;
			float posY = m_isGrid ? m_cellRectPool[m_bottomMostCellIndex].anchoredPosition.y : 0;
			int additionalRows = 0;

			List<T> dataList = new List<T>(Data);

			while ((m_cellRectPool[m_topMostCellIndex].position.y - (m_cellHeight / 2f)) > m_recyclableViewBounds.max.y && m_currentItemCount < Data.Length)
			{
				if (m_isGrid)
				{
					if (++m_bottomMostCellColumn >= m_columns)
					{
						rowsShifted++;
						m_bottomMostCellColumn = 0;
						posY = m_cellRectPool[m_bottomMostCellIndex].anchoredPosition.y - m_cellHeight;
						additionalRows++;
					}

					float posX = m_bottomMostCellColumn * m_cellWidth;
					m_cellRectPool[m_topMostCellIndex].anchoredPosition = new Vector2(posX, posY);

					if (++m_topMostCellColumn >= m_columns)
					{
						m_topMostCellColumn = 0;
						additionalRows--;
					}
				}
				else
				{
					posY = m_cellRectPool[m_bottomMostCellIndex].anchoredPosition.y - m_cellHeight;
					m_cellRectPool[m_topMostCellIndex].anchoredPosition = new Vector2(m_cellRectPool[m_topMostCellIndex].anchoredPosition.x, posY);
				}

				//m_cellComponentPool[m_topMostCellIndex].ConfigureCell(m_currentItemCount, dataList);

				m_bottomMostCellIndex = m_topMostCellIndex;
				m_topMostCellIndex = (m_topMostCellIndex + 1) % m_cellRectPool.Count;

				m_currentItemCount++;
				if (!m_isGrid) rowsShifted++;
			}

			if (m_isGrid)
			{
				m_content.sizeDelta += additionalRows * Vector2.up * m_cellHeight;
				if (additionalRows > 0) rowsShifted -= additionalRows;
			}

			float shiftAmount = rowsShifted * m_cellHeight;
			m_cellRectPool.ForEach((cell) => cell.anchoredPosition += Vector2.up * shiftAmount);
			m_content.anchoredPosition -= Vector2.up * shiftAmount;

			m_isRecycling = false;
			return -new Vector2(0, shiftAmount);
		}

		private Vector2 RecycleBottomToTop()
		{
			m_isRecycling = true;
			int rowsShifted = 0;
			float posY = m_isGrid ? m_cellRectPool[m_topMostCellIndex].anchoredPosition.y : 0;
			int additionalRows = 0;

			List<T> dataList = new List<T>(Data);

			while ((m_cellRectPool[m_bottomMostCellIndex].position.y + (m_cellHeight / 2f)) < m_recyclableViewBounds.min.y && m_currentItemCount > m_cellRectPool.Count)
			{
				if (m_isGrid)
				{
					if (--m_topMostCellColumn < 0)
					{
						rowsShifted++;
						m_topMostCellColumn = m_columns - 1;
						posY = m_cellRectPool[m_topMostCellIndex].anchoredPosition.y + m_cellHeight;
						additionalRows++;
					}

					float posX = m_topMostCellColumn * m_cellWidth;
					m_cellRectPool[m_bottomMostCellIndex].anchoredPosition = new Vector2(posX, posY);

					if (--m_bottomMostCellColumn < 0)
					{
						m_bottomMostCellColumn = m_columns - 1;
						additionalRows--;
					}
				}
				else
				{
					posY = m_cellRectPool[m_topMostCellIndex].anchoredPosition.y + m_cellHeight;
					m_cellRectPool[m_bottomMostCellIndex].anchoredPosition = new Vector2(m_cellRectPool[m_bottomMostCellIndex].anchoredPosition.x, posY);
					rowsShifted++;
				}

				m_currentItemCount--;
				//m_cellComponentPool[m_bottomMostCellIndex].ConfigureCell(m_currentItemCount - m_cellRectPool.Count, dataList);

				m_topMostCellIndex = m_bottomMostCellIndex;
				m_bottomMostCellIndex = (m_bottomMostCellIndex - 1 + m_cellRectPool.Count) % m_cellRectPool.Count;
			}

			if (m_isGrid)
			{
				m_content.sizeDelta += additionalRows * Vector2.up * m_cellHeight;
				if (additionalRows > 0) rowsShifted -= additionalRows;
			}

			float shiftAmount = rowsShifted * m_cellHeight;
			m_cellRectPool.ForEach((cell) => cell.anchoredPosition -= Vector2.up * shiftAmount);
			m_content.anchoredPosition += Vector2.up * shiftAmount;

			m_isRecycling = false;
			return new Vector2(0, shiftAmount);
		}

		private void SetTopAnchor(RectTransform rectTransform)
		{
			float width = rectTransform.rect.width;
			float height = rectTransform.rect.height;
			rectTransform.anchorMin = new Vector2(0.5f, 1);
			rectTransform.anchorMax = new Vector2(0.5f, 1);
			rectTransform.pivot = new Vector2(0.5f, 1);
			rectTransform.sizeDelta = new Vector2(width, height);
		}

		private void SetTopLeftAnchor(RectTransform rectTransform)
		{
			float width = rectTransform.rect.width;
			float height = rectTransform.rect.height;
			rectTransform.anchorMin = new Vector2(0, 1);
			rectTransform.anchorMax = new Vector2(0, 1);
			rectTransform.pivot = new Vector2(0, 1);
			rectTransform.sizeDelta = new Vector2(width, height);
		}

		protected virtual void OnDrawGizmos()
		{
			Gizmos.color = Color.green;
			Gizmos.DrawLine(m_recyclableViewBounds.min - new Vector3(2000, 0), m_recyclableViewBounds.min + new Vector3(2000, 0));
			Gizmos.color = Color.red;
			Gizmos.DrawLine(m_recyclableViewBounds.max - new Vector3(2000, 0), m_recyclableViewBounds.max + new Vector3(2000, 0));
		}
	}
}
