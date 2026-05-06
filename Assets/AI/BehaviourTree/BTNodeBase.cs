using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTrees
{
	public enum EBTNodeState
	{
		STATE_RUNNING,
		STATE_SUCSESS, 
		STATE_FAILURE
	}

	public abstract class BTNodeBase
	{
		protected EBTNodeState m_nodeState;

		protected BTNodeBase m_parentNode;
		protected List<BTNodeBase> m_childNodes = new List<BTNodeBase>();

		private Dictionary<string, object> m_dataCtx = new Dictionary<string, object>();

		private bool m_hasBeganEvaluation = false;

        #region Consturctors

        public BTNodeBase()
		{
			m_parentNode = null;
		}

		/// <summary>
		/// Fills the nodes list of children on construction
		/// </summary>
		public BTNodeBase(List<BTNodeBase> children)
		{
			foreach (BTNodeBase i in children)
			{
				AddChild(i);
			}
		}
        #endregion

        public virtual EBTNodeState Evaluate(float t)
		{
			// Dirty flag to allow logic for a notes first evaluation
			if (!m_hasBeganEvaluation)
			{
				m_hasBeganEvaluation = true;
				OnFirstEvaluate();
			}

			return EBTNodeState.STATE_FAILURE;
		} 

		protected virtual void OnFirstEvaluate() {}

        private void AddChild(BTNodeBase node)
        {
            node.SetParentNode(this);
            m_childNodes.Add(node);
        }

        public void SetParentNode(BTNodeBase node)
		{
			m_parentNode = node;
		}

		public BTNodeBase GetParentNode()
		{
			return m_parentNode;
		}

        #region Node Data

		/// <summary>
		/// Recursively travels up the tree until the root, then adds data to the roots context dictionary.
		/// </summary>
		/// <param name="key">The name of the data</param>
		/// <param name="value">The data value</param>
        public void SetData(string key, object value)
		{
			// Find the root of the tree to set the data
			var current = this;
			while (current.m_parentNode != null)
			{
				current = current.m_parentNode;
			}

			current.m_dataCtx[key] = value;
		}

		/// <summary>
		/// Recursively travels up the tree until the root, then checks the root for data.
		/// </summary>
		/// <param name="key">The name of the data</param>
		/// <param name="value">The data value</param>
		public object GetData(string key)
		{
			BTNodeBase current = this;

			while (current != null)
			{
				// Check if the current node has the data
				if (current.m_dataCtx.TryGetValue(key, out object value))
				{
					return value;
				}

				// If not, move up to the parent and loop again
				current = current.m_parentNode;
			}

			// The root was hit but no data could be found
			return null;
		}

		public bool ClearData(string key)
		{
			if (m_dataCtx.ContainsKey(key))
			{
				m_dataCtx.Remove(key);
				return true;
			}

			BTNodeBase node = this;
			while (node != null)
			{
				bool cleared = node.ClearData(key);

				if (cleared)
					return true;

				node = node.m_parentNode;
			}

			return false;
		}
        #endregion
    }
}
