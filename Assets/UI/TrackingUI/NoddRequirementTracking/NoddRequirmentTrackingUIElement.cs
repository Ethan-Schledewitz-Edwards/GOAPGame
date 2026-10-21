using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UISystem.TrackingUI
{
	public class NoddRequirmentTrackingUIElement : TrackingUIElementBase
	{
		[SerializeField] private TypeRequirementElement m_typeRequirementPrefab;


		public override void ElementReleased()
		{
			gameObject.SetActive(true);
		}

		public override void ElementReturned()
		{
			ResetDynamicPosition();
			//SetActorValues(0, 0);
			gameObject.SetActive(false);
		}
	}
}
