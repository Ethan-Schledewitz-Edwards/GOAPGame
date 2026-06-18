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

		protected string GetContextKey(string propertyName)
		{
			return $"{NodeID}_{propertyName}";
		}

		public EBTNodeState Evaluate(AIContext context, float t)
		{
			string beganKey = GetContextKey("HasBegan");
			if (!context.GetData<bool>(beganKey))
			{
				context.SetData<bool>(beganKey, true);
				OnFirstEvaluate(context);
			}

			return OnUpdate(context, t);
		}

		protected abstract EBTNodeState OnUpdate(AIContext context, float t);

		public virtual void Reset(AIContext context)
		{
			string beganKey = GetContextKey("HasBegan");
			context.SetData<bool>(beganKey, false);

			foreach (BTNodeBase child in m_childNodes)
			{
				child.Reset(context);
			}
		}

		protected virtual void OnFirstEvaluate(AIContext context) { } // Only if children need it

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
