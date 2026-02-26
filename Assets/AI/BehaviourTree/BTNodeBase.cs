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

		public BTNodeBase()
		{
			m_parentNode = null;
		}

		public BTNodeBase(List<BTNodeBase> children)
		{
			foreach (BTNodeBase i in children)
			{
				AddChild(i);
			}
		}

		public void SetParent(BTNodeBase node)
		{
			m_parentNode = node;
		}

		public BTNodeBase GetParent()
		{
			return m_parentNode;
		}

		private void AddChild(BTNodeBase node) 
		{
			node.SetParent(this);
			m_childNodes.Add(node);
		}

		public virtual EBTNodeState Evaluate() => EBTNodeState.STATE_FAILURE;

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
	}
}
