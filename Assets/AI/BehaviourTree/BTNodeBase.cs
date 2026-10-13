
using AI.AIContext;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTrees
{
	public enum EBTNodeState
	{
		RUNNING,
		SUCSESS,
		FAILURE
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

		public BTNodeBase(List<BTNodeBase> children) : this()
		{
			foreach (BTNodeBase child in children)
			{
				AddChild(child);
			}
		}


		public EBTNodeState EvaluateNode(ref AIContext context, float t)
		{
			if (context.ActiveNodeIDs.Add(NodeID))
			{
				OnFirstEvaluate(ref context);
			}

			return OnNodeEvaluated(ref context, t);
		}

		protected abstract EBTNodeState OnNodeEvaluated(
			ref AIContext context, float t);

		protected abstract void OnFirstEvaluate(ref AIContext context);


		public void ExitNode(ref AIContext context)
		{
			// Clean up descendants before this node.
			foreach (BTNodeBase child in m_childNodes)
			{
				child.ExitNode(ref context);
			}

			// Only exit nodes that are currently active.
			if (context.ActiveNodeIDs.Remove(NodeID))
			{
				OnNodeExited(ref context);
			}
		}

		protected abstract void OnNodeExited(ref AIContext context);

		public void ResetNode(ref AIContext context)
		{
			foreach (BTNodeBase child in m_childNodes)
			{
				child.ResetNode(ref context);
			}

			context.ActiveNodeIDs.Remove(NodeID);

			OnNodeReset(ref context);
		}

		protected abstract void OnNodeReset(ref AIContext context);

		private void AddChild(BTNodeBase node)
		{
			if (node == null)
			{
				Debug.LogError($"Cannot add a null child to behaviour tree node {NodeID}.");
				return;
			}

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
