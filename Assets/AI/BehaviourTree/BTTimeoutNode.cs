using AI.AIContext;
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

		protected override EBTNodeState OnNodeEvaluated(ref AIContext context, float t)
		{
			float timeElapsed = context.Timeout + t;
			context.Timeout = timeElapsed;

			if (timeElapsed >= m_duration)
			{
				Transform executor = context.ExecutorTransform;
				if (executor != null)
					Debug.Log($"{executor}'s task timed out", executor);

				return EBTNodeState.FAILURE;
			}

			return m_childNodes[0].EvaluateNode(ref context, t);
		}

		protected override void OnFirstEvaluate(ref AIContext context)
		{
			context.Timeout = 0f;
		}

		protected override void OnNodeExited(ref AIContext context) 
		{
			context.Timeout = 0f;

			foreach (BTNodeBase child in m_childNodes)
			{
				child.ExitNode(ref context);
			}
		}

		protected override void OnNodeReset(ref AIContext context)
		{
			context.Timeout = 0f;
		}
	}
}
