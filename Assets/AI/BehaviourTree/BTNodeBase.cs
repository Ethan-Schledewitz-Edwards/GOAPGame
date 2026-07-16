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
		private const string c_firstEvauatedKey = "FirstEvaluated";

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
		public BTNodeBase(List<BTNodeBase> children)
		{
			foreach (BTNodeBase i in children)
			{
				AddChild(i);
			}
		}

		protected string GetContextKey(string propertyName)
		{
			return $"{NodeID}_{propertyName}";
		}

		public EBTNodeState EvaluateNode(AIContext context, float t)
		{
			string beganKey = GetContextKey(c_firstEvauatedKey);
			if (!context.GetData<bool>(beganKey))
			{
				context.SetData<bool>(beganKey, true);
				OnFirstEvaluate(context);
			}

			return OnNodeEvaluated(context, t);
		}

		protected abstract EBTNodeState OnNodeEvaluated(AIContext context, float t);

		protected abstract void OnFirstEvaluate(AIContext context);

		public void ExitNode(AIContext context) 
		{
			context.ClearData(GetContextKey(c_firstEvauatedKey));

			OnNodeExited(context);
		}

		protected abstract void OnNodeExited(AIContext context);

		public void ResetNode(AIContext context)
		{
			string beganKey = GetContextKey(c_firstEvauatedKey);
			context.SetData<bool>(beganKey, false);

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
