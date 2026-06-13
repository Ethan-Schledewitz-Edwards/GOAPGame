using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UISystems.RecyclingScrollRect
{
	public abstract class RecyclingScrollRectBase<T> : MonoBehaviour
	{
		protected virtual T[] m_data { get; private set; }

		[Header("Cells")]
		[SerializeField] private GameObject m_cellPrefab;
		[SerializeField] private float m_desiredCellWidth = 256, m_desiredCellHeight = 256;
		[SerializeField] private float m_cellMarginX = 16, m_cellMarginY = 16;
		[SerializeField] private float m_cellPaddingY = 12;
		[SerializeField] private bool m_isGrid = true;
		[SerializeField] private int m_columns = 4;
		[SerializeField] private float m_minPoolCoverage = 1.5f;
		[SerializeField] private int m_minPoolSize = 10;

		[Header("Rect Properties")]
		[Tooltip("The padding multiplier outside the viewport where cells remain active before being recycled.")]
		[SerializeField] private float m_recyclingThreshold = 0.1f;
		[SerializeField] private RectTransform m_viewport, m_content;

		// System
		private List<RectTransform> m_cellRectPool = new List<RectTransform>();

		private float m_cellWidth, m_cellHeight;
		private float m_spacingX;

		private int m_topCellIndex;
		private int m_bottomCellIndex;
		private int m_poolSize;

		protected virtual void Start()
		{
			InitializeSystem();
		}

		public void InitializeSystem()
		{
			if (m_data == null || m_data.Length == 0) 
				return;

			SetTopAnchor(m_content);
			m_content.anchoredPosition = Vector2.zero;

			m_cellHeight = m_desiredCellHeight;
			InitializeCellPool();

			// Set content height based on total rows generated
			int totalRows = Mathf.CeilToInt((float)m_data.Length / (m_isGrid ? m_columns : 1));
			float totalContentHeight = m_cellMarginY + (totalRows * m_cellHeight) + (Mathf.Max(0, totalRows - 1) * m_cellPaddingY);
			m_content.sizeDelta = new Vector2(m_content.sizeDelta.x, totalContentHeight);
			SetTopAnchor(m_content);
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

			float viewportWidth = m_viewport.rect.width;
			float totalDesiredWidth = m_columns * m_desiredCellWidth;

			if (totalDesiredWidth > viewportWidth)
			{
				// Force cells to shrink to fit
				m_cellWidth = viewportWidth / m_columns;
				m_spacingX = 0f;
			}
			else
			{
				// Keep the cells at their target desired width.
				m_cellWidth = m_desiredCellWidth;

				// Distribute the extra space
				m_spacingX = m_columns > 1 ? (viewportWidth - totalDesiredWidth) / (m_columns - 1) : 0f;
			}

			float requiredRectCoverage = m_viewport.rect.height * m_minPoolCoverage;
			float currentRectCoverage = 0;
			int currentPoolSize = 0;
			while ((currentPoolSize < m_minPoolSize || currentRectCoverage < requiredRectCoverage) && currentPoolSize < m_data.Length)
			{
				GameObject spawnedObj = Instantiate(m_cellPrefab);
				RectTransform itemRect = spawnedObj.GetComponent<RectTransform>();
				itemRect.SetParent(m_content, false);
				m_cellRectPool.Add(itemRect);

				currentPoolSize++;
				int currentRows = Mathf.CeilToInt((float)currentPoolSize / (m_isGrid ? m_columns : 1));
				currentRectCoverage = m_cellMarginY + (currentRows * m_cellHeight) + ((currentRows - 1) * m_cellPaddingY);
			}

			m_poolSize = m_cellRectPool.Count;
			m_topCellIndex = 0;
			m_bottomCellIndex = m_poolSize - 1;

			// Update visual locations matching data offsets
			for (int i = 0; i < m_poolSize; i++)
			{
				PositionCellAtIndex(m_cellRectPool[i], i);
			}
		}

		public void OnScrollValueChanged(Vector2 scrollDirection)
		{
			if (m_cellRectPool == null || m_cellRectPool.Count == 0) 
				return;

			float localViewportHeight = m_viewport.rect.height;
			float thresholdBuffer = m_recyclingThreshold * localViewportHeight;

			float topVisibleLimit = m_content.anchoredPosition.y - thresholdBuffer;
			float bottomVisibleLimit = m_content.anchoredPosition.y + localViewportHeight + thresholdBuffer;

			bool processingRecycle = true;
			int safetyLoopCounter = 0;
			while (processingRecycle && safetyLoopCounter < m_poolSize)
			{
				processingRecycle = false;
				safetyLoopCounter++;

				float topCellBottomY = Mathf.Abs(GetCellAnchoredY(m_topCellIndex)) + m_cellHeight;
				float bottomCellTopY = Mathf.Abs(GetCellAnchoredY(m_bottomCellIndex));

				// Check Top-To-Bottom recycling trigger
				if (topCellBottomY < topVisibleLimit && m_bottomCellIndex < m_data.Length - 1)
				{
					int activePoolIndex = m_topCellIndex % m_poolSize;
					m_topCellIndex++;
					m_bottomCellIndex++;

					PositionCellAtIndex(m_cellRectPool[activePoolIndex], m_bottomCellIndex);
					processingRecycle = true;
				}
				// Check Bottom-To-Top recycling trigger
				else if (bottomCellTopY > bottomVisibleLimit && m_topCellIndex > 0)
				{
					int activePoolIndex = m_bottomCellIndex % m_poolSize;
					m_topCellIndex--;
					m_bottomCellIndex--;

					PositionCellAtIndex(m_cellRectPool[activePoolIndex], m_topCellIndex);
					processingRecycle = true;
				}
			}
		}

		private void PositionCellAtIndex(RectTransform cellRect, int virtualIndex)
		{
			cellRect.name = $"Cell_{virtualIndex}";
			cellRect.anchoredPosition = new Vector2(GetCellAnchoredX(virtualIndex), GetCellAnchoredY(virtualIndex));
			cellRect.sizeDelta = new Vector2(m_cellWidth, m_cellHeight);

			if (cellRect.TryGetComponent<IRecyclableCell<T>>(out var cellComp))
			{
				cellComp.ConfigureCell(virtualIndex, m_data);
			}
		}

		private float GetCellAnchoredX(int virtualIndex)
		{
			if (!m_isGrid) 
				return 0f;

			int column = virtualIndex % m_columns;
			return column * (m_cellWidth + m_spacingX);
		}

		private float GetCellAnchoredY(int virtualIndex)
		{
			int row = m_isGrid ? (virtualIndex / m_columns) : virtualIndex;
			return -m_cellMarginY - (row * (m_cellHeight + m_cellPaddingY));
		}

		private void SetTopAnchor(RectTransform rectTransform)
		{
			Vector2 originalSize = rectTransform.sizeDelta;
			rectTransform.anchorMin = new Vector2(0.5f, 1);
			rectTransform.anchorMax = new Vector2(0.5f, 1);
			rectTransform.pivot = new Vector2(0.5f, 1);
			rectTransform.sizeDelta = originalSize;
		}

		private void SetTopLeftAnchor(RectTransform rectTransform)
		{
			Vector2 originalSize = rectTransform.sizeDelta;
			rectTransform.anchorMin = new Vector2(0, 1);
			rectTransform.anchorMax = new Vector2(0, 1);
			rectTransform.pivot = new Vector2(0, 1);
			rectTransform.sizeDelta = originalSize;
		}
	}
}
