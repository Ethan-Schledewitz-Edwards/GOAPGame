using BehaviourTrees;
using InventorySystem;
using InventorySystem.Items;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Graphs;
using UnityEngine;

[RequireComponent(typeof(BoxCollider), typeof(BluerprintInventoryComponent))]
public class BlueprintIO : InteractableObjectBase, IInteractableStructure<BlueprintIO>
{
	private static BehaviourTree s_cachedBlueprintBT;

	private const string c_interactionLayer = "Interaction";
	private const float c_cancelationDuration = 3.0f;
	private const float c_maxShakeMagnitude = 0.1f;
	private const float c_maxShakeFrequency = 15.0f;

	public event Action<BlueprintIO> BlueprintCompleted;
	public event Action<BlueprintIO> BlueprintCanceled;

	public int BlueprintID { get; private set; }
	public int SettlementID { get; private set; }
	public Vector3 Position { get; private set; }
	public Quaternion Rotation { get; private set; }

	private BluerprintInventoryComponent m_bluerprintInventory;
	private ItemQuantity[] m_requiredItems;

	[SerializeField] private float m_maxCapacity = 4f;
	[SerializeField] private float m_actorsAssigned = 0f;
	public float MaxCapacity => m_maxCapacity;
	public float ActorsAssigned => m_actorsAssigned;
	public override bool UseFormationRadius { get => false; }

	public bool IsBeingCanceled { get; private set; }
	private Coroutine m_cancelationCoroutine;
	private float m_shakeStrength;

	private void Awake()
	{
		if (s_cachedBlueprintBT == null)
		{
			BehaviourTree tree = new BehaviourTree();
			BTNodeBase root = new BTSequenceNode(new List<BTNodeBase>
			{
				new FindItemTask(),
				new MoveToTargetDataTask(),
				new InteractWithTargetTask(),
				new MoveToTargetDataTask(),
				new DepositTask(),
				// Go to item
				// Pickup item
				// return
				// Deposit
			});
			tree.SetTree(root);
			s_cachedBlueprintBT = tree;
		}

		gameObject.layer = LayerMask.NameToLayer(c_interactionLayer);

		m_bluerprintInventory = GetComponent<BluerprintInventoryComponent>();
		m_bluerprintInventory.BlueprintItemsAchieved += CompleteBlueprint;
	}

	private void OnDestroy()
	{
		m_bluerprintInventory.BlueprintItemsAchieved -= CompleteBlueprint;
	}

	public override void UpdateSpeed(int extra)
	{

	}

	public void AssignActor(out BlueprintIO structure)
	{
		if (ActorsAssigned < MaxCapacity)
		{
			m_actorsAssigned++;
		}

		structure = this;
	}

	public override void TryInteract(IInteractor interactor)
	{
		base.TryInteract(interactor);

		BehaviourTreeExecutor executor = interactor.Transform.GetComponent<BehaviourTreeExecutor>();
		if (executor != null)
		{
			Transform requiredItem = FindRequiredItem();
			if (requiredItem != null)
				executor?.AIContext.SetData<Transform>("TargetTransform", requiredItem);
		}
	}

	public void InitializeBlueprint(int blueprintID, int settlementID, ItemQuantity[] requiredItems, Vector3 position, Quaternion rotation)
	{
		BlueprintID = blueprintID;
		SettlementID = settlementID;
		Position = position;
		Rotation = rotation;
		m_bluerprintInventory.InitializeBlueprintInventory(requiredItems);
		m_requiredItems = requiredItems;
	}

	private void CompleteBlueprint()
	{
		Debug.Log($"A blueprint of Blueprint ID:{BlueprintID} was completed in settlement:{SettlementID}.");
		BlueprintCompleted?.Invoke(this);
	}

	public void CancleBlueprint()
	{
		Debug.Log($"A blueprint of Blueprint ID:{BlueprintID} was canceld in settlement:{SettlementID}.");
		BlueprintCanceled?.Invoke(this);

		foreach (InventorySlot slot in m_bluerprintInventory.Slots)
		{
			slot.RemoveFromStack(slot.AmountInSlot, transform.position);
		}

		Destroy(gameObject);
	}

	public void BeginCancelation()
	{
		IsBeingCanceled = true;

		if (m_cancelationCoroutine != null)
			StopCoroutine(m_cancelationCoroutine);

		m_cancelationCoroutine = StartCoroutine(ShakeCoroutine(c_cancelationDuration, 
			c_maxShakeMagnitude, 
			c_maxShakeFrequency));
	}

	public void StopCancelation()
	{
		IsBeingCanceled = false;

		if (m_cancelationCoroutine != null)
			StopCoroutine(m_cancelationCoroutine);
	}

	#region Utility

	private Transform FindRequiredItem()
	{
		Transform globalNearest = null;
		float minDistanceSqr = float.MaxValue;
		Vector3 currentPos = transform.position;

		foreach (ItemQuantity requiredItem in m_requiredItems)
		{
			if (!m_bluerprintInventory.GetItemTypeSatisfied(requiredItem))
			{
				Transform candidate = SearchForItem(requiredItem.itemType);

				if (candidate != null)
				{
					float distSqr = (candidate.position - currentPos).sqrMagnitude;
					if (distSqr < minDistanceSqr)
					{
						minDistanceSqr = distSqr;
						globalNearest = candidate;
					}
				}
			}
		}

		return globalNearest;
	}

	private Transform SearchForItem(ItemData itemData)
	{
		/*
		Vector2Int centerChunk = WorldToChunkCoord(transform.position);
		int chunkRange = Mathf.CeilToInt(searchRadius / WorldBuilder.s_ChunkSize.x);

		var chunksToSearch = GetChunksInRadius(transform.position, searchRadius);

		Transform nearest = null;
		float minDistanceSqr = float.MaxValue;

		foreach (var chunk in chunksToSearch)
		{
			foreach (var item in chunk.Items)
			{
				if (item.Data == itemData)
				{
					float distSqr = (item.transform.position - transform.position).sqrMagnitude;
					if (distSqr < minDistanceSqr)
					{
						minDistanceSqr = distSqr;
						nearest = item.transform;
					}
				}
			}
		}
		*/

		return null;
	}

	private IEnumerator ShakeCoroutine(float duration, float magnitude, float frequency)
	{
		Vector3 originalPosition = transform.position;
		float elapsedTime = 0f;

		// Generate a random starting point in the Perlin noise map so shakes feel unique
		float randomSeedX = UnityEngine.Random.Range(0f, 100f);
		float randomSeedY = UnityEngine.Random.Range(0f, 100f);

		while (elapsedTime < duration)
		{
			elapsedTime += Time.deltaTime;

			float progress = Mathf.Clamp01(elapsedTime / duration);
			float currentMagnitude = magnitude * progress;

			float noiseX = Mathf.PerlinNoise(randomSeedX + elapsedTime * frequency, 0f) * 2f - 1f;
			float noiseY = Mathf.PerlinNoise(0f, randomSeedY + elapsedTime * frequency) * 2f - 1f;

			transform.localPosition = new Vector3(
				originalPosition.x + (noiseX * currentMagnitude),
				originalPosition.y,
				originalPosition.z
			);

			yield return null;
		}

		// Always force reset back to the exact initial position
		transform.localPosition = originalPosition;
		m_cancelationCoroutine = null;

		CancleBlueprint();
	}

	public override BehaviourTree GetBehaviourTree() => s_cachedBlueprintBT;

	#endregion
}
