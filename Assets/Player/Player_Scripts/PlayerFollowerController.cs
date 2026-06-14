using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerFollowerController : PlayerWorldControllerBase, IInputHandler
{
	[SerializeField] protected LayerMask m_actorLayers;

	private List<Actor> m_followers = new List<Actor>();

	private bool m_isSummonHeld = false;

	#region Input Methods

	public override void Subscribe()
	{
		InputManager.Controls.Player.Look.performed += OnMouseInput;

		InputManager.Controls.Player.Primary.performed += OnPrimaryInput;

		InputManager.Controls.Player.Secondary.performed += OnSecondaryInput;
		InputManager.Controls.Player.Secondary.canceled += OnSecondaryInput;
	}

	public override void UnSubscribe()
	{
		InputManager.Controls.Player.Look.performed -= OnMouseInput;

		InputManager.Controls.Player.Primary.performed -= OnPrimaryInput;

		InputManager.Controls.Player.Secondary.performed -= OnSecondaryInput;
		InputManager.Controls.Player.Secondary.canceled -= OnSecondaryInput;
	}

	private void OnMouseInput(InputAction.CallbackContext context)
	{
		m_mousePosition = context.ReadValue<Vector2>();
	}

	private void OnPrimaryInput(InputAction.CallbackContext context)
	{
		TryAssignActor(m_cursorVisualizer.transform.position);
	}

	private void OnSecondaryInput(InputAction.CallbackContext context)
	{
		m_isSummonHeld = context.ReadValueAsButton();
	}
	#endregion

	#region Monobehaviour Methods

	protected override void Update()
	{
		base.Update();

		if (m_isSummonHeld)
		{
			Select(m_cursorVisualizer.transform.position);
		}
	}
	#endregion

	#region Actions

	private void Select(Vector3 position)
	{
		// Try to select actors
		Collider[] hitColliders = Physics.OverlapSphere(position, c_SelectionRadius, m_actorLayers);

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

	#endregion

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

	// Finds the follower closest to the player
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
}
