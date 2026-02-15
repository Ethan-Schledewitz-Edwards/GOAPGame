using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour, IInputHandler
{
	// Constants
	private const float k_HitEpsilon = 0.02f; // Min distance to wall
	private const float k_GroundCheckDist = 0.01f;
	private const float k_StopEpsilon = 0.001f; // Stop when <= this speed
	private const int k_MaxBounces = 4; // Max number of iterations per frame
	private const int k_MaxConcurrentPlanes = 8; // Max number of planes to collide with at once

	// Ground Movement
	private const float k_walkingSpeed = 5f;
	private const float k_friction = 3.8f;
	private const float k_acceleration = 8.5f;
	private const float k_maxSpeed = 7f;

	private const float k_stepHeight = 0.7f;

	// Normals with Y greater than this are walkable
	private const float k_maxWalkableAngle = 45;
	private static float minWalkableNormalY = Mathf.Cos(Mathf.Deg2Rad * k_maxWalkableAngle);

	// Jumping
	private const float k_jumpForce = 7;

	// Air Movement Values
	private const float k_airSpeed = .5f;
	private const float k_airAcceleration = 20;
	private const float k_gravity = 15;

	// Collision Values
	private LayerMask collisionLayerMask;
	private const float k_horizontalSize = .5f;
	private const float k_verticalSize = 2;

	#region System Vars

	// Movement type
	[SerializeField] private EMovementMode movementMode;
	public enum EMovementMode
	{
		Standard,
		Animation,
	}

	// Components
	private Rigidbody m_rb;
	private CapsuleCollider m_col;
	private Player m_player;

	// Fields
	private Vector2 inputDir;
	private Vector3 velocity;
	private Vector3 position;
	private Quaternion rotation;

	private bool isGrounded;
	private bool isJumpPressed;

	private int framesStuck = 0;
	private bool justJumped;

	private GameObject surfaceObject;

	#endregion

	#region Initialization Methods

	void Awake()
	{
		m_rb = GetComponent<Rigidbody>();
		m_col = GetComponent<CapsuleCollider>();
		m_player = GetComponent<Player>();

		m_rb.isKinematic = true;
		m_rb.freezeRotation = true;

		collisionLayerMask = LayerMask.GetMask("Default", "Environment", "Interaction");
		m_col.layerOverridePriority = 100;

		LayerMask colliderMask = collisionLayerMask | LayerMask.GetMask("Trigger");
		m_col.excludeLayers = ~colliderMask;
		m_col.includeLayers = colliderMask;

		UpdateCollider();
	}

	void Start()
	{
		((IInputHandler)this).SetControlsSubscription(true);
	}
	#endregion

	#region Unity Callbacks

	private void OnDestroy()
	{
		((IInputHandler)this).SetControlsSubscription(false);
	}

	private void FixedUpdate()
	{
		UpdateCollider();

		if (InputManager.ControlMode != InputManager.ControlType.Player)
		{
			inputDir = Vector2.zero;
		}

		switch (movementMode)
		{
			case EMovementMode.Standard:
				PlayerMovement();
				break;

			case EMovementMode.Animation:
				AnimationMovement();
				break;
		}

		if (!float.IsNaN(position.x) && !float.IsNaN(position.y) && !float.IsNaN(position.z))
		{
			m_rb.MovePosition(position);
		}
		else
		{
			Debug.LogError("Position is NaN, skipping MovePosition.");
			velocity = Vector3.zero;
		}

		UpdateCollider();

		justJumped = false;
	}
	#endregion

	#region Input Handlers

	private void OnMoveInput(InputAction.CallbackContext context)
	{
		inputDir = context.ReadValue<Vector2>();
	}

	private void OnJumpInput(InputAction.CallbackContext context)
	{
		isJumpPressed = context.ReadValueAsButton();

		if (isJumpPressed && isGrounded)
			Jump();
	}

	public void Subscribe()
	{
		InputManager.Controls.Player.Movement.performed += OnMoveInput;
		InputManager.Controls.Player.Movement.canceled += OnMoveInput;
		InputManager.Controls.Player.Jump.performed += OnJumpInput;
		InputManager.Controls.Player.Jump.canceled += OnJumpInput;
	}

	public void UnSubscribe()
	{
		InputManager.Controls.Player.Movement.performed -= OnMoveInput;
		InputManager.Controls.Player.Movement.canceled -= OnMoveInput;
		InputManager.Controls.Player.Jump.performed -= OnJumpInput;
		InputManager.Controls.Player.Jump.canceled -= OnJumpInput;

		inputDir = Vector2.zero;
	}
	#endregion

	#region Input Actions

	private void Jump()
	{
		velocity.y += k_jumpForce;

		isGrounded = false;
		justJumped = true;
	}

	#endregion

	#region Collision

	/// <summary>
	/// Casts the players hull in a position
	/// </summary>
	private bool CastHull(Vector3 position, Vector3 direction, float maxDist, out RaycastHit hitInfo)
	{
		direction.Normalize();

		float halfHeight = GetColliderHeight() / 2f;

		bool hit = Physics.BoxCast
		(
			position + Vector3.up * halfHeight,
			new Vector3(k_horizontalSize / 2f, halfHeight, k_horizontalSize / 2f),
			direction,
			out hitInfo,
			Quaternion.identity,
			maxDist + k_HitEpsilon,
			collisionLayerMask,
			QueryTriggerInteraction.Ignore
		);

		// Back up a little
		if (hit)
		{
			float nDot = -Vector3.Dot(hitInfo.normal, direction);
			float backup = k_HitEpsilon / nDot;
			hitInfo.distance -= backup;
		}
		else
		{
			hitInfo.distance = maxDist;
		}
		if (hitInfo.distance < 0) hitInfo.distance = 0;

		return hit;
	}

	private bool StuckCheck()
	{
		float halfHeight = GetColliderHeight() / 2f;
		Collider[] colliders = Physics.OverlapBox
		(
			position + Vector3.up * halfHeight, new Vector3
			(
				k_horizontalSize / 2f - k_HitEpsilon,
				halfHeight - k_HitEpsilon,
				k_horizontalSize / 2f - k_HitEpsilon
			),
			Quaternion.identity,
			collisionLayerMask,
			QueryTriggerInteraction.Ignore
		);

		if (colliders.Length > 0)
		{
			++framesStuck;

			Debug.LogWarning("Player stuck!");

			if (framesStuck > 5)
			{
				Debug.Log("Wow, you're REALLY stuck.");
				velocity = Vector3.zero;
				position += Vector3.up * 0.5f;
			}

			if (Physics.ComputePenetration(
				m_col,
				position,
				transform.rotation,
				colliders[0],
				colliders[0].transform.position,
				colliders[0].transform.rotation,
				out Vector3 dir,
				out float dist
			))
			{
				position += dir * (dist + k_HitEpsilon * 2.0f);
				velocity = Vector3.zero;
			}
			else
			{
				velocity = Vector3.zero;
				position += Vector3.up * 0.5f;
			}

			return true;
		}

		framesStuck = 0;

		return false;
	}

	private void CollideAndSlide(ref Vector3 position, ref Vector3 velocity)
	{
		Vector3 startVelocity = velocity;
		Vector3 velocityBeforePlanes = startVelocity;

		// When we collide with multiple planes at once (crease)
		Vector3[] planes = new Vector3[k_MaxConcurrentPlanes];
		int planeCount = 0;

		float time = Time.fixedDeltaTime; // The amount of time remaining in the frame, decreases with each iteration
		int bounceCount;
		for (bounceCount = 0; bounceCount < k_MaxBounces; ++bounceCount)
		{
			float speed = velocity.magnitude;

			if (speed <= k_StopEpsilon)
			{
				velocity = Vector3.zero;
				break;
			}

			// Try to move in this direction
			Vector3 direction = velocity.normalized;
			float maxDist = speed * time;
			if (CastHull(position, direction, maxDist, out RaycastHit hit))
			{
				if (hit.distance > 0)
				{
					// Move to where it collided
					position += direction * hit.distance;

					// Decrease time based on how far it travelled
					float fraction = hit.distance / maxDist;

					if (fraction > 1)
					{
						Debug.LogWarning("Fract too high");
						fraction = 1;
					}

					time -= fraction * time;

					planeCount = 0;
					velocityBeforePlanes = velocity;
				}

				if (planeCount >= k_MaxConcurrentPlanes)
				{
					Debug.LogWarning("Colliding with too many planes at once");
					velocity = Vector3.zero;
					break;
				}

				planes[planeCount] = hit.normal;
				++planeCount;

				// Clip velocity to each plane
				bool conflictingPlanes = false;
				for (int j = 0; j < planeCount; ++j)
				{
					velocity = Vector3.ProjectOnPlane(velocityBeforePlanes, planes[j]);

					if (planeCount == 1)
					{
						velocityBeforePlanes = velocity;
					}

					// Check if the velocity is against any other planes
					for (int k = 0; k < planeCount; ++k)
					{
						if (j != k) // No point in checking the same plane we just clipped to
						{
							if (Vector3.Dot(velocity, planes[k]) < 0) // Moving into the plane, BAD!
							{
								conflictingPlanes = true;
								break;
							}
						}
					}

					if (!conflictingPlanes) break;// Use the first good plane
				}

				// No good planes
				if (conflictingPlanes)
				{
					if (planeCount == 2)
					{
						// Cross product of two planes is the only direction to go
						Vector3 dir = Vector3.Cross(planes[0], planes[1]).normalized;

						// Go in that direction
						velocity = dir * Vector3.Dot(dir, velocity);
					}
					else
					{
						velocity = Vector3.zero;
						break;
					}
				}
			}
			else
			{
				// Move rigibody according to velocity
				position += direction * hit.distance;
				break;
			}

			// Stop tiny oscillations
			if (Vector3.Dot(velocity, startVelocity) < 0)
			{
				//Debug.Log("Oscillation");
				velocity = Vector3.zero;
				break;
			}

			if (time < 0)
			{
				Debug.Log("Outta time");
				break; // outta time
			}
		}

		if (bounceCount >= k_MaxBounces)
		{
			Debug.LogWarning("Bounces exceeded");
		}
	}
	#endregion

	#region Movement Methods

	private void Friction(float friction)
	{
		float speed = velocity.magnitude;

		float control = Mathf.Max(speed, k_maxSpeed);

		float newSpeed = Mathf.Max(speed - (control * friction * Time.fixedDeltaTime), 0);

		if (speed != 0)
		{
			float mult = newSpeed / speed;
			velocity *= mult;
		}
	}

	private void Accelerate(Vector3 dir, float acceleration, float maxSpeed)
	{
		float add = acceleration * maxSpeed * Time.fixedDeltaTime;

		// Clamp added velocity in acceleration direction
		float speed = Vector3.Dot(dir, velocity);

		if (speed + add > maxSpeed)
		{
			add = Mathf.Max(maxSpeed - speed, 0);
		}

		velocity += add * dir;
	}

	private void PlayerMovement()
	{
		// Project inputDir onto the XZ plane
		Vector3 moveDir = (Vector3.forward * inputDir.y + Vector3.right * inputDir.x).normalized;

		if (moveDir.sqrMagnitude > 0.01f)
		{
			HandleRotation(moveDir);
		}

		isGrounded = GroundCheck(position, out surfaceObject);

		// Pick movement method
		if (isGrounded)
		{
			GroundMove(moveDir);
		}
		else
		{
			AirMove(moveDir);
		}

		StuckCheck();
	}

	private void AnimationMovement()
	{
		isGrounded = GroundCheck(position, out surfaceObject);
		StuckCheck();
	}

	private void GroundMove(Vector3 moveDir)
	{
		velocity.y = 0;

		Friction(k_friction);

		float desiredSpeed = k_walkingSpeed * inputDir.magnitude;
		Accelerate(moveDir, k_acceleration, desiredSpeed);

		// Clamp Speed
		float speed = velocity.magnitude;
		if (speed > k_walkingSpeed)
		{
			float mult = k_walkingSpeed / speed;
			velocity *= mult;
		}

		if (velocity.sqrMagnitude == 0)
		{
			return;
		}

		// Try to step up/down
		StepMove();
	}

	private bool StepMove()
	{
		// Do the regular move
		Vector3 prevPosition = position;
		Vector3 prevVelocity = velocity;
		CollideAndSlide(ref prevPosition, ref prevVelocity);

		// Move down to ground
		CastHull(prevPosition, Vector3.down, k_stepHeight, out RaycastHit downHit1);
		Vector3 groundedPos = prevPosition + Vector3.down * downHit1.distance;

		bool regGrounded = GroundCheck(groundedPos, out GameObject _);

		// Only step down onto ground
		if (regGrounded)
		{
			prevPosition = groundedPos;
		}

		// Move up and try another move, stepping over stuff
		CastHull(position, Vector3.up, k_stepHeight, out RaycastHit upHit);
		Vector3 steppedPosition = position + Vector3.up * upHit.distance;
		Vector3 steppVelocity = velocity;

		CollideAndSlide(ref steppedPosition, ref steppVelocity);

		// Move back down
		CastHull(steppedPosition, Vector3.down, k_stepHeight + upHit.distance, out RaycastHit downHit);
		steppedPosition += Vector3.down * downHit.distance;

		bool stepGrounded = GroundCheck(steppedPosition, out GameObject stepSurface);

		// If we stepped onto air, just do the regular move
		if (!stepGrounded)
		{
			position = prevPosition;
			velocity = prevVelocity;
			return false;
		}

		// Otherwise, pick the move that goes the furthest
		if (Vector3.Distance(position, prevPosition) >= Vector3.Distance(position, steppedPosition))
		{
			position = prevPosition;
			velocity = prevVelocity;
			return false;
		}

		position = steppedPosition;
		velocity = steppVelocity;
		surfaceObject = stepSurface;
		isGrounded = stepGrounded;

		velocity.y = Mathf.Max(velocity.y, prevVelocity.y); // funny quake ramp jumps
		return true;
	}

	private void AirMove(Vector3 moveDir)
	{
		float desiredSpeed = k_airSpeed * inputDir.magnitude;
		Accelerate(moveDir, k_airAcceleration, desiredSpeed);

		float yVel = velocity.y;
		velocity.y -= k_gravity * Time.fixedDeltaTime / 2f;
		CollideAndSlide(ref position, ref velocity);
		velocity.y -= k_gravity * Time.fixedDeltaTime / 2f;

		isGrounded = GroundCheck(position, out surfaceObject);
	}

	public void Teleport(Vector3 position)
	{
		this.position = position;
		transform.position = this.position;
	}

	public void Stop()
	{
		velocity = Vector3.zero;
	}
	#endregion

	#region Rotation Methods

	private void HandleRotation(Vector3 moveDir)
	{
		Quaternion prevRot = rotation;

		Quaternion targetRotation = Quaternion.LookRotation(moveDir);
		rotation = Quaternion.Slerp(prevRot, targetRotation, Time.deltaTime * 10f);

		// Rotate player mesh
		m_player.PlayerMesh.rotation = rotation;
	}

	#endregion

	#region Utility

	private void UpdateCollider()
	{
		float h = GetColliderHeight();
		m_col.height = k_verticalSize;
		m_col.radius = k_horizontalSize;
		m_col.center = new Vector3(0, h / 2.0f, 0);
	}

	public float GetColliderHeight()
	{
		return k_verticalSize;
	}

	private IEnumerator AnimWait(float animTime)
	{
		yield return new WaitForSeconds(animTime);
		movementMode = EMovementMode.Standard;
	}

	private bool GroundCheck(Vector3 position, out GameObject surfaceObject)
	{
		surfaceObject = null;
		if (justJumped) return false;

		if (CastHull(position, Vector3.down, k_GroundCheckDist, out RaycastHit hit))
		{
			if (hit.normal.y > minWalkableNormalY)
			{
				surfaceObject = hit.collider.gameObject;
				return true;
			}
		}
		else
		{
			return false;
		}

		// If we're on a slope, check if any point on the player is on the ground
		// Source uses 4 box checks, but I'm really lazy so I'll just do a raycast.
		if (Physics.Raycast(position,
			Vector3.down,
			out RaycastHit hit2,
			k_GroundCheckDist * 2,
			collisionLayerMask,
			QueryTriggerInteraction.Ignore
		))
		{
			if (hit2.normal.y > minWalkableNormalY)
			{
				surfaceObject = hit2.collider.gameObject;
				return true;
			}
		}

		return false;
	}
	#endregion
}