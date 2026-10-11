using System;
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
		public readonly string NodeID = Guid.NewGuid().ToString();

		protected BTNodeBase m_parentNode;
		protected List<BTNodeBase> m_childNodes = new List<BTNodeBase>();

        public BTNodeBase()
		{
			m_parentNode = null;
		}

		/// <summary>
		/// Fills the nodes list of children on construction
		/// </summary>
		public BTNodeBase(List<BTNodeBase> children) : this()
		{
			foreach (BTNodeBase i in children)
			{
				AddChild(i);
			}
		}

		public EBTNodeState EvaluateNode(ref AIContext context, float t)
		{
			if (context.CurrentNodeID != NodeID)
			{
				context.CurrentNodeID = NodeID;
				OnFirstEvaluate(ref context);
			}

			return OnNodeEvaluated(ref context, t);
		}

		protected abstract EBTNodeState OnNodeEvaluated(ref AIContext context, float t);

		protected abstract void OnFirstEvaluate(ref AIContext context);

		public void ExitNode(ref AIContext context) 
		{
			context.CurrentNodeID = string.Empty;
			OnNodeExited(ref context);
		}

		protected abstract void OnNodeExited(ref AIContext context);

		public void ResetNode(AIContext context)
		{
			context.CurrentNodeID = string.Empty;

			foreach (BTNodeBase child in m_childNodes)
			{
				child.ResetNode(context);
			}

			OnNodeReset(ref context);
		}

		protected abstract void OnNodeReset(ref AIContext context);

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
	}
}
