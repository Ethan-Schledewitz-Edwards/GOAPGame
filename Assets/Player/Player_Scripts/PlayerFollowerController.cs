using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerFollowerController : PlayerWorldControllerBase
{
	private List<Actor> m_followers = new List<Actor>();

	private bool m_isSummonHeld = false;

	private LayerMask m_actorLayers;

	protected override void Awake()
	{
		base.Awake();
		if (m_actorLayers == 0) 
			m_actorLayers = LayerMask.GetMask("Actor");
	}


	protected override void OnPrimaryFireInput(InputAction.CallbackContext context)
	{
		TryAssignActor(m_cursorWorldPosition);
	}

	protected override void OnSecondaryFireInput(InputAction.CallbackContext context) 
	{
		m_isSummonHeld = context.ReadValueAsButton();

		if (m_isSummonHeld)
			m_cursorVisualizer.GrowCursor();
		else
			m_cursorVisualizer.ShrinkCursor();
	}

	protected override void OnCycleInput(InputAction.CallbackContext context) { }

	private void Select(Vector3 position)
	{
		float selectionRadius = m_cursorVisualizer.SelectionRadius;

		// Try to select actors
		Collider[] hitColliders = Physics.OverlapSphere(position, selectionRadius, m_actorLayers);
		if (hitColliders.Length != 0)
		{
			foreach (Collider i in hitColliders)
			{
				Actor actor = i.GetComponent<Actor>();
				if (actor != null)
				{
					AddFollower(actor);
				}
			}
		}
	}

	private void TryAssignActor(Vector3 throwPosition)
	{
		// Remove the closest follower and throw them at the cursor
		Actor followerToThrow = FindClosestFollower();
		if (followerToThrow != null) 
		{ 
			RemoveFollower(followerToThrow);
			followerToThrow.InvestigatePosition(throwPosition);
		}
	}

	private void AddFollower(Actor newFollower)
	{
		if (m_followers.Contains(newFollower))
			return;

		// Update systems to include new actor
		m_followers.Add(newFollower);
		newFollower.FollowPlayer(this.transform);
	}

	private void RemoveFollower(Actor actor)
	{
		if (m_followers.Contains(actor))
		{
			m_followers.Remove(actor);
		}
	}

	/// <summary>
	/// Finds and returns the follower actor closest to the current actor's position.
	/// </summary>
	/// <returns>The closest follower actor, or null if no followers are present.</returns>
	private Actor FindClosestFollower()
	{
		Actor closestFollower = null;

		float closestDist = Mathf.Infinity;
		foreach (Actor actor in m_followers)
		{
			if(actor == null) continue;

			float dist = Vector3.Distance(transform.position, actor.transform.position);
			if (dist < closestDist)
			{
				closestFollower = actor;
				closestDist = dist;
			}
		}

		return closestFollower;
	}

	protected override void RefreshCursor()
	{
		base.RefreshCursor();

		if (m_isSummonHeld)
			Select(m_cursorWorldPosition);
	}
}
