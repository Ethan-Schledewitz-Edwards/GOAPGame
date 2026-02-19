using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace BehaviourTrees
{
	public enum EBTNodeState
	{
		STATE_RUNNING,
		STATE_SUCSESS, 
		STATE_FAILURE
	}

	public class BTNode
	{
		protected EBTNodeState m_nodeState;

		protected BTNode m_parentNode;
		protected List<BTNode> m_childNodes = new List<BTNode>();

		private Dictionary<string, object> m_dataCtx = new Dictionary<string, object>();

		public BTNode()
		{
			m_parentNode = null;
		}

		public BTNode(List<BTNode> children)
		{
			foreach (BTNode i in children)
			{
				AddChild(i);
			}
		}

		public void SetParent(BTNode node)
		{
			m_parentNode = node;
		}

		public BTNode GetParent()
		{
			return m_parentNode;
		}

		private void AddChild(BTNode node) 
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

			BTNode node = m_parentNode;
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

			BTNode node = m_parentNode;
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
