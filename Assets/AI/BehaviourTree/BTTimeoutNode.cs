using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTrees
{
	public class BTTimeoutNode : BTNodeBase
	{
		private const string c_timerKey = "TimeElapsed";

		private float m_duration;

		/// <summary>
		/// Times out a child node and sets a specific flag in the AIContext if it fails.
		/// </summary>
		public BTTimeoutNode(BTNodeBase child, float duration) : base(new List<BTNodeBase> { child } ) 
		{
			m_duration = duration;
		}

		protected override EBTNodeState OnNodeEvaluated(AIContext context, float t)
		{
			string timeKey = GetContextKey(c_timerKey);

			float timeElapsed = context.GetData<float>(timeKey) + t;
			context.SetData<float>(timeKey, timeElapsed);

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
			context.ClearData(GetContextKey(c_timerKey));

			context.SetData<float>(c_timerKey, 0f);
		}

		protected override void OnNodeExited(AIContext context) 
		{
			context.ClearData(GetContextKey(c_timerKey));
		}

		protected override void OnNodeReset(AIContext context)
		{
			context.ClearData(GetContextKey(c_timerKey));
		}
	}
}
