using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerFollowerController : MonoBehaviour, IInputHandler
{
	// Constants
	private const float k_selectionRadius = 1.0f;

	private Camera m_mainCamera;
	private Vector2 m_mousePosition;

	[SerializeField] private LayerMask m_cursorBlockingLayers;
	[SerializeField] private LayerMask m_actorLayers;
	[SerializeField] private Transform m_cursorVisualizer;

	private List<Actor> m_followers = new List<Actor>();

	private bool m_isSummonHeld = false;

	#region Initialization Methods

	public void Start()
	{
		m_mainCamera = Camera.main;

		((IInputHandler)this).SetControlsSubscription(true);
	}

	private void OnDestroy()
	{
		((IInputHandler)this).SetControlsSubscription(false);
	}
	#endregion

	#region Input Methods

	public void Subscribe()
	{
		InputManager.Controls.Player.Look.performed += OnMouseInput;
		InputManager.Controls.Player.Primary.performed += OnPrimaryInput;
		InputManager.Controls.Player.Primary.canceled += OnPrimaryInput;
		InputManager.Controls.Player.Secondary.performed += OnSecondaryInput;
	}

	public void UnSubscribe()
	{
		InputManager.Controls.Player.Look.performed -= OnMouseInput;
		InputManager.Controls.Player.Primary.performed -= OnPrimaryInput;
		InputManager.Controls.Player.Primary.canceled -= OnPrimaryInput;
		InputManager.Controls.Player.Secondary.performed -= OnSecondaryInput;
	}

	private void OnMouseInput(InputAction.CallbackContext context)
	{
		m_mousePosition = context.ReadValue<Vector2>();
	}

	private void OnPrimaryInput(InputAction.CallbackContext context)
	{
		m_isSummonHeld = context.ReadValueAsButton();
	}

	private void OnSecondaryInput(InputAction.CallbackContext context)
	{
		TryAssignActor(m_cursorVisualizer.position);
	}
	#endregion

	#region Monobehaviour Methods

	private void Update()
	{
		Ray ray = m_mainCamera.ScreenPointToRay(m_mousePosition);

		if (Physics.Raycast(ray, out RaycastHit hitData, 100f, m_cursorBlockingLayers))
		{
			m_cursorVisualizer.position = hitData.point;
		}

		if (m_isSummonHeld)
		{
			Select(m_cursorVisualizer.position);
		}
	}
	#endregion

	#region Actions
	private void Select(Vector3 position)
	{
		// Try to select actors
		Collider[] hitColliders = Physics.OverlapSphere(position, k_selectionRadius, m_actorLayers);

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
			followerToThrow.GoToDestination(throwPosition);
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
