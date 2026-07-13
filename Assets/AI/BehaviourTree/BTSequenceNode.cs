using System.Collections.Generic;

namespace BehaviourTrees
{
	/// <summary>
	/// Returns success only if all children succeed, returning failure immediately if any child fails.
	/// </summary>
	public class BTSequenceNode : BTNodeBase
	{
		public BTSequenceNode() : base() { }
		public BTSequenceNode(List<BTNodeBase> children) : base(children) { }

		protected override EBTNodeState OnUpdate(AIContext context, float t)
		{
			if (m_childNodes == null || m_childNodes.Count == 0)
				return EBTNodeState.STATE_SUCSESS;

			string sequenceKey = GetContextKey("SequenceIndex");
			int currentChildIndex = context.GetData<int>(sequenceKey);

			while (currentChildIndex < m_childNodes.Count)
			{
				BTNodeBase currentChild = m_childNodes[currentChildIndex];
				EBTNodeState childState = currentChild.Evaluate(context, t);

				switch (childState)
				{
					case EBTNodeState.STATE_FAILURE:
						context.SetData<int>(sequenceKey, 0); // Reset for next execution cycle
						return EBTNodeState.STATE_FAILURE;

					case EBTNodeState.STATE_SUCSESS:
						currentChild.OnExit(context);
						currentChildIndex++;
						context.SetData<int>(sequenceKey, currentChildIndex); // Advance to the next task
						break;

					case EBTNodeState.STATE_RUNNING:
						return EBTNodeState.STATE_RUNNING; // Resume next frame
				}
			}

			context.SetData<int>(sequenceKey, 0);
			return EBTNodeState.STATE_SUCSESS;
		}

		protected override void OnFirstEvaluate(AIContext context) 
		{
			context.SetData<int>(GetContextKey("SequenceIndex"), 0);
		}
	}
}
