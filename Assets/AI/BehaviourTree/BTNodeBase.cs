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

        public virtual EBTNodeState Evaluate(AIContext aIContext, float t)
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
    }
}
