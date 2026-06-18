using TMPro;
using UnityEngine;

public class ClockElement : UIElement
{
	[SerializeField] private TextMeshProUGUI m_clockText;

	private void Start()
	{
		(int currentHour, int currentMinute) = GameManager.Instance.GetTimeOfDay();
		OnClockUpdated(currentHour, currentMinute);
	}

	private void OnEnable()
	{
		if (GameManager.Instance != null)
		{
			GameManager.Instance.ClockUpdated += OnClockUpdated;
		}
	}

	private void OnDisable()
	{
		if (GameManager.Instance != null)
			GameManager.Instance.ClockUpdated -= OnClockUpdated;
	}

	private void OnClockUpdated(int hour, int minute)
	{
		string amPm = hour >= 12 ? "PM" : "AM";
		int displayHour = hour % 12;
		if (displayHour == 0)
			displayHour = 12;

		m_clockText.text = $"{displayHour}:{minute:D2} {amPm}";
	}
}
