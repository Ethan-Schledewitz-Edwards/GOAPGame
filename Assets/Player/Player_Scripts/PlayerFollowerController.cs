using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

namespace Player.Core
{
	public class PlayerFollowerController : PlayerWorldControllerBase
	{

		private WaitForSeconds startDelay = new WaitForSeconds(0.2f);

		private LayerMask m_actorLayers;

		private bool m_isSummonHeld = false;

		private Vector3 m_cursorWorldPosition;
		private List<Actor> m_activeFollowers = new List<Actor>();

		protected override void Awake()
		{
			base.Awake();

			if (m_actorLayers == 0)
				m_actorLayers = LayerMask.GetMask("Actor");
		}

		private void Start()
		{
			if (ActorManager.Instance != null)
				ActorManager.Instance.FollowingActorLoaded += AddFollower;
		}

		private void OnDestroy()
		{
			if (ActorManager.Instance != null)
				ActorManager.Instance.FollowingActorLoaded -= AddFollower;
		}

		protected override void OnPrimaryFireInput(InputAction.CallbackContext context)
		{
			if (context.ReadValueAsButton())
				TryAssignActor(m_cursorWorldPosition);
		}

		protected override void OnSecondaryFireInput(InputAction.CallbackContext context)
		{
			m_isSummonHeld = context.ReadValueAsButton();

			if (m_isSummonHeld)
				m_worldControllerManager.CursorVisualizer.GrowCursor();
			else
				m_worldControllerManager.CursorVisualizer.ShrinkCursor();
		}

		protected override void OnCycleInput(InputAction.CallbackContext context) { }

		private void Select(Vector3 position)
		{
			float selectionRadius = m_worldControllerManager.CursorVisualizer.SelectionRadius;

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
			if (m_activeFollowers.Contains(newFollower))
				return;

			Debug.Log("Added Follower eh");

			// Update systems to include new actor
			m_activeFollowers.Add(newFollower);
			newFollower.FollowPlayer(this.transform);
		}

		private void RemoveFollower(Actor actor)
		{
			if (m_activeFollowers.Contains(actor))
			{
				m_activeFollowers.Remove(actor);
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
			foreach (Actor actor in m_activeFollowers)
			{
				if (actor == null) continue;

				float dist = Vector3.Distance(transform.position, actor.transform.position);
				if (dist < closestDist)
				{
					closestFollower = actor;
					closestDist = dist;
				}
			}

			return closestFollower;
		}

		public override void RefreshCursor(Vector3 worldPosition)
		{
			if (InputManager.ControlMode == InputManager.ControlType.Player)
			{
				m_cursorWorldPosition = worldPosition;

				if (m_isSummonHeld)
					Select(worldPosition);
			}
		}
	}
}