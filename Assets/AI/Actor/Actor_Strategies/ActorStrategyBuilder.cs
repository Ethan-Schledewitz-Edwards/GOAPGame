using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Actor), typeof(GOAPAgent), typeof(AIPathing))]
public class ActorStrategyBuilder : MonoBehaviour
{
	private Actor m_actor;
	private AIPathing m_aiPathing;
	private GOAPAgent m_goapAgent;

	private void Awake()
	{
		m_actor = GetComponent<Actor>();
		m_aiPathing = GetComponent<AIPathing>();
		m_goapAgent = GetComponent<GOAPAgent>();

		EstablishBaseBeliefs();
		EstablishBaseActions();
		EstablishBaseGoals();
	}

	private void OnEnable()
	{
		if(m_actor != null)
		{
			m_actor.OnSettlementUpdated += UpdateSettlement;
			m_actor.OnHouseUpdated += UpdateHouse;
		}
	}

	private void OnDestroy()
	{
		if (m_actor != null)
		{
			m_actor.OnSettlementUpdated -= UpdateSettlement;
			m_actor.OnHouseUpdated -= UpdateHouse;
		}
	}

	#region Base Knowledge

	private void EstablishBaseBeliefs()
	{
		Dictionary<string, AgentBelief> beliefs = new Dictionary<string, AgentBelief>();
		BeliefFactory beliefFactory = new BeliefFactory(m_goapAgent, beliefs);

		beliefFactory.AddBelief("None", () => false);
		beliefFactory.AddBelief("ActorIdle", () => !m_aiPathing.NavAgent.hasPath);
		beliefFactory.AddBelief("ActorMoving", () => m_aiPathing.NavAgent.hasPath);

		beliefFactory.AddBelief("HungerLow", () => m_actor.ActorHealth.Hunger < 45.0f);
		beliefFactory.AddBelief("HungerHealthy", () => m_actor.ActorHealth.Hunger > 60.0f);
		beliefFactory.AddBelief("RestLow", () => m_actor.ActorHealth.Rest < 20.0f);
		beliefFactory.AddBelief("RestHealthy", () => m_actor.ActorHealth.Rest > 50.0f);
		beliefFactory.AddBelief("HapinessLow", () => m_actor.ActorHealth.Hapiness < 40.0f);
		beliefFactory.AddBelief("HapinessHealthy", () => m_actor.ActorHealth.Hapiness > 60.0f);

		m_goapAgent.UpdateBeliefs(beliefs);
	}

	private void EstablishBaseActions()
	{
		HashSet<AgentAction> actions = new HashSet<AgentAction>();

		actions.Add(new AgentAction.ActionBuilder("Relax")
			.BuildWithStrategy(new IdleStrategy(5))
			.AddEffect(m_goapAgent.Beliefs["None"])
			.Build());

		actions.Add(new AgentAction.ActionBuilder("Wander")
			.BuildWithStrategy(new WanderStrategy(m_goapAgent, 10))
			.AddEffect(m_goapAgent.Beliefs["ActorMoving"])
			.Build());

		m_goapAgent.UpdateActions(actions);
	}

	private void EstablishBaseGoals()
	{
		HashSet<AgentGoal> goals = new HashSet<AgentGoal>();

		goals.Add(new AgentGoal.GoalBuilder("Idle")
			.BuildWithPriority(1)
			.BuildWithDesiredEffect(m_goapAgent.Beliefs["None"])
			.Build());

		goals.Add(new AgentGoal.GoalBuilder("Wander")
			.BuildWithPriority(1)
			.BuildWithDesiredEffect(m_goapAgent.Beliefs["ActorMoving"])
			.Build());

		goals.Add(new AgentGoal.GoalBuilder("KeepFed")
			.BuildWithPriority(2)
			.BuildWithDesiredEffect(m_goapAgent.Beliefs["HungerHealthy"])
			.Build());

		goals.Add(new AgentGoal.GoalBuilder("Sleep")
			.BuildWithPriority(2)
			.BuildWithDesiredEffect(m_goapAgent.Beliefs["RestHealthy"])
			.Build());

		m_goapAgent.UpdateGoals(goals);
	}
	#endregion

	public void UpdateSettlement(int settlementID)
	{
		// Have this one make a behaviour to return to settlement or something
	}

	public void UpdateHouse(int houseID)
	{
		int settlementID = m_actor.SettlementID;
		Vector3 housePos = SettlementManager.s_WorldSettlements[settlementID].ActorHouses[houseID].GetInteractionPositon();

		HashSet<AgentAction> actions = new HashSet<AgentAction>();
		actions.Add(new AgentAction.ActionBuilder("MoveToHouse")
			.BuildWithStrategy(new MoveStrategy(m_goapAgent, () => housePos))
			.AddEffect(m_goapAgent.Beliefs["ActorAtHouse"])
			.Build());
		m_goapAgent.UpdateActions(actions);


		Dictionary<string, AgentBelief> beliefs = new Dictionary<string, AgentBelief>();
		BeliefFactory beliefFactory = new BeliefFactory(m_goapAgent, beliefs);
		beliefFactory.AddPosBelief("ActorAtHome", 3f, housePos);
		m_goapAgent.UpdateBeliefs(beliefs);
	}

	public void SeekFood()
	{

		////SettlementManager.Instance.WorldSettlements[m_settlementID].TryFindResourceStorage	

		//HashSet<AgentAction> actions = new HashSet<AgentAction>();

		//actions.Add(new AgentAction.ActionBuilder("MoveToFood")
		//	.BuildWithStrategy(new MoveStrategy(m_goapAgent, () => m_foodStorage.GetInteractionPositon()))
		//	.AddEffect(m_goapAgent.Beliefs["ActorAtFoodStorage"])
		//	.Build());

		//actions.Add(new AgentAction.ActionBuilder("Eat")
		//	.BuildWithStrategy(new EatStrategy(m_goapAgent, m_foodStorage, 5))
		//	.AddPrecondition(m_goapAgent.Beliefs["ActorAtFoodStorage"])
		//	.AddEffect(m_goapAgent.Beliefs["HungerHealthy"])
		//	.Build());

		//Dictionary<string, AgentBelief> beliefs = new Dictionary<string, AgentBelief>();
		//BeliefFactory beliefFactory = new BeliefFactory(m_goapAgent, beliefs);

		//beliefFactory.AddPosBelief("ActorAtFoodStorage", 3f, m_foodStorage.GetInteractionPositon());

		//m_goapAgent.UpdateActions(actions);
	}
}
