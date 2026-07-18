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
		protected readonly string m_firstEvaluatedKey;

		protected BTNodeBase m_parentNode;
		protected List<BTNodeBase> m_childNodes = new List<BTNodeBase>();

        public BTNodeBase()
		{
			m_parentNode = null;
			m_firstEvaluatedKey = $"{NodeID}_FirstEvaluated";
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

		public EBTNodeState EvaluateNode(AIContext context, float t)
		{
			if (!context.GetData<bool>(m_firstEvaluatedKey))
			{
				context.SetData<bool>(m_firstEvaluatedKey, true);
				OnFirstEvaluate(context);
			}

			return OnNodeEvaluated(context, t);
		}

		protected abstract EBTNodeState OnNodeEvaluated(AIContext context, float t);

		protected abstract void OnFirstEvaluate(AIContext context);

		public void ExitNode(AIContext context) 
		{
			context.ClearData(m_firstEvaluatedKey);
			OnNodeExited(context);
		}

		protected abstract void OnNodeExited(AIContext context);

		public void ResetNode(AIContext context)
		{
			context.SetData<bool>(m_firstEvaluatedKey, false);

			foreach (BTNodeBase child in m_childNodes)
			{
				child.ResetNode(context);
			}

			OnNodeReset(context);
		}

		protected abstract void OnNodeReset(AIContext context);

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
