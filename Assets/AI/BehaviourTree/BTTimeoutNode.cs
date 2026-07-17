using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTrees
{
	public class BTTimeoutNode : BTNodeBase
	{
		private readonly string m_timeoutKey;
		private readonly float m_duration;

		/// <summary>
		/// Times out a child node and sets a specific flag in the AIContext if it fails.
		/// </summary>
		public BTTimeoutNode(BTNodeBase child, float duration) : base(new List<BTNodeBase> { child } ) 
		{
			m_duration = duration;
			m_timeoutKey = $"{NodeID}_Timeout";
		}

		protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
		{
			float timeElapsed = context.GetData<float>(m_timeoutKey) + t;
			context.SetData<float>(m_timeoutKey, timeElapsed);

			if (timeElapsed >= m_duration)
			{
				// Timeout!
				m_childNodes[0].ResetNode(context);

				return EBTNodeState.STATE_FAILURE;
			}

			return m_childNodes[0].EvaluateNode(context, t);
		}

		protected override void OnFirstEvaluate(AIContext context)
		{
			context.ClearData(m_timeoutKey);
			context.SetData<float>(m_timeoutKey, 0f);
		}

		protected override void OnNodeExited(AIContext context) 
		{
			context.ClearData(m_timeoutKey);
		}

		protected override void OnNodeReset(AIContext context)
		{
			context.ClearData(m_timeoutKey);
		}
	}
}
