using System.Collections.Generic;

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

        public virtual EBTNodeState Evaluate()
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

        public void SetData(string key, object value)
		{
			m_dataCtx[key] = value;
		}

		public object GetData(string key)
		{
			object value = null;

			if(m_dataCtx.TryGetValue(key, out value))
				return value;

			BTNodeBase node = m_parentNode;
			while (node != null) 
			{ 
				value = node.GetData(key);
				if(value != null) 
					return value;

				node = node.m_parentNode;
			}

			return null;
		}

		public bool ClearData(string key)
		{
			if (m_dataCtx.ContainsKey(key))
			{
				m_dataCtx.Remove(key);
				return true;
			}

			BTNodeBase node = m_parentNode;
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
