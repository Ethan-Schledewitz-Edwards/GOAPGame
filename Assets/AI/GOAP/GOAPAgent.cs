using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class GOAPAgent : MonoBehaviour
{
	private IGoapPlanner m_goapPlanner;

	private AgentGoal m_lastGoal;
	private AgentGoal m_currentGoal;
	private AgentPlan m_actionPlan;
	private AgentAction m_currentAction;

	public Dictionary<string, AgentBelief> Beliefs { get; private set; } = new Dictionary<string, AgentBelief>();
	public HashSet<AgentAction> Actions { get; private set; } = new HashSet<AgentAction>();
	public HashSet<AgentGoal> Goals { get; private set; } = new HashSet<AgentGoal>();

	// Events
	public event Action<Vector3> OnFoundDestination;
	public event Action OnClearDestination;

	private void Awake()
	{
		m_goapPlanner = new GoapPlanner();
	}

	public void UpdateBeliefs(Dictionary<string, AgentBelief> newBeliefs)
	{
		if (newBeliefs == null) 
			return;

		foreach (var kvp in newBeliefs)
		{
			Beliefs[kvp.Key] = kvp.Value;
		}
	}

	public void UpdateActions(IEnumerable<AgentAction> newActions)
	{
		if (newActions == null) return;

		Actions.UnionWith(newActions);
	}

	public void UpdateGoals(IEnumerable<AgentGoal> newGoals)
	{
		if (newGoals == null) return;

		Goals.UnionWith(newGoals);
	}

	private void HandleTargetChanged()
	{
		m_currentAction = null;
		m_currentGoal = null;
	}

	public void TickGoapPlanner(float t)
	{
		// If we don't have an action, try to get the next one from the CURRENT plan
		if (m_currentAction == null && m_actionPlan != null && m_actionPlan.Actions.Count > 0)
		{
			m_currentAction = m_actionPlan.Actions.Pop();

			if (m_currentAction.ActionPreconditions.All(b => b.Evaluate()))
			{
				m_currentAction.StartAction();
			}
			else
			{
				// Plan failed - preconditions not met for next step
				m_currentAction = null;
				m_actionPlan = null;
			}
		}

		// Update the actors plan and current action if they dont have one
		if (m_currentAction == null)
		{
			CalculatePlan();

			if (m_actionPlan != null && m_actionPlan.Actions.Count > 0)
			{
				// Reset path
				NotifyClearDestination();

				m_currentGoal = m_actionPlan.GoalToAcheive;
				Debug.Log($"Goal: {m_currentGoal.GoalName} with {m_actionPlan.Actions.Count} actions in plan");

				m_currentAction = m_actionPlan.Actions.Pop();
				Debug.Log($"Popped action: {m_currentAction.ActionName}");

				// Verify all precodnitions
				if (m_currentAction.ActionPreconditions.All(b => b.Evaluate()))
				{
					m_currentAction.StartAction();
				}
				else
				{
					m_currentAction = null;
					m_currentGoal = null;
				}
			}
		}

		// If we have a current action, execute it
		if (m_actionPlan != null && m_currentAction != null)
		{
			m_currentAction.TickAction(t);

			if (m_currentAction.Complete)
			{
				Debug.Log($"{m_currentAction.ActionName} is complete");
				m_currentAction.StopAction();

				m_currentAction = null;

				if (m_actionPlan.Actions.Count == 0)
				{
					Debug.Log($"{this.name}'s plan is complete!");
					m_lastGoal = m_currentGoal;
					m_currentGoal = null;
					m_actionPlan = null;
				}
			}
		}
	}

	private void CalculatePlan()
	{
		float priorityLevel = m_currentGoal?.Priority ?? 0;

		HashSet<AgentGoal> goalsToCheck = Goals;

		// Only check higher priority goals if the actor already has one
		if (m_currentGoal != null)
		{
			goalsToCheck = new HashSet<AgentGoal>(Goals.Where(g => g.Priority > priorityLevel));
		}

		AgentPlan potentialPlan = m_goapPlanner.Plan(this, goalsToCheck, m_lastGoal);
		if (potentialPlan != null)
		{
			m_actionPlan = potentialPlan;
		}
	}

	public void NotifyNewDestination(Vector3 destination)
	{
		OnFoundDestination?.Invoke(destination);
	}

	public void NotifyClearDestination()
	{
		OnClearDestination?.Invoke();
	}
}
