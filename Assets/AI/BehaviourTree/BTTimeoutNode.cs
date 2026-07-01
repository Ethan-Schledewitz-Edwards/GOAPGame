using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTrees
{
	public class BTTimeoutNode : BTNodeBase
	{
		private float m_timeoutDuration;
		private string m_blackboardFlag;

		/// <summary>
		/// Times out a child node and sets a specific flag in the AIContext if it fails.
		/// </summary>
		public BTTimeoutNode(BTNodeBase child, float duration, string contextFlag = "Timeout")
			: base(new System.Collections.Generic.List<BTNodeBase> { child })
		{
			m_timeoutDuration = duration;
			m_blackboardFlag = contextFlag;
		}

		protected override EBTNodeState OnUpdate(AIContext context, float t)
		{
			string beganKey = GetContextKey("HasBegan");
			string timeKey = GetContextKey("TimeElapsed");

			if (!context.GetData<bool>(beganKey))
			{
				context.SetData<bool>(beganKey, true);
				context.SetData<float>(timeKey, 0f);
			}

			float timeElapsed = context.GetData<float>(timeKey) + t;
			context.SetData<float>(timeKey, timeElapsed);

			if (timeElapsed >= m_timeoutDuration)
			{
				// Timeout!
				context.SetData<bool>(m_blackboardFlag, true);
				m_childNodes[0].Reset(context);

				return EBTNodeState.STATE_FAILURE;
			}

			return m_childNodes[0].Evaluate(context, t);
		}

		public override void Reset(AIContext context)
		{
			base.Reset(context);
			context.ClearData(GetContextKey("TimeElapsed"));
		}

		protected override void OnFirstEvaluate(AIContext context) { }
	}
}
