using System.Collections.Generic;

namespace BehaviourTrees
{
	/// <summary>
	/// Evaluates children in order and returns the state of the first one that does not fail.
	/// </summary>
	public class BTSelectorNode : BTNodeBase
	{
        #region Constructors

        public BTSelectorNode() : base() { }
		public BTSelectorNode(List<BTNodeBase> children) : base(children) { }
		#endregion

		protected override EBTNodeState OnUpdate(AIContext context, float t)
		{
			foreach (BTNodeBase i in m_childNodes)
			{
				switch (i.Evaluate(context, t))
				{
					case EBTNodeState.STATE_FAILURE:
						continue;

					case EBTNodeState.STATE_SUCSESS:
						return EBTNodeState.STATE_SUCSESS;

					case EBTNodeState.STATE_RUNNING:
						return EBTNodeState.STATE_RUNNING;
				}
			}

			return EBTNodeState.STATE_FAILURE;
		}

		protected override void OnFirstEvaluate(AIContext context) { }
	}
}