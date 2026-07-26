using TMPro;
using UnityEngine;
using WorldLighting;

public class ClockElement : UIElement
{
	[SerializeField] private ITimeOfDayController m_timeOfDayController;
	[SerializeField] private TextMeshProUGUI m_clockText;

	private void Start()
	{
		(int currentHour, int currentMinute) = m_timeOfDayController.GetTimeOfDay();
		OnClockUpdated(currentHour, currentMinute);
	}

	private void OnEnable()
	{
		if (m_timeOfDayController != null)
		{
			m_timeOfDayController.ClockUpdated += OnClockUpdated;
		}
	}

	private void OnDisable()
	{
		if (m_timeOfDayController != null)
		{
			m_timeOfDayController.ClockUpdated -= OnClockUpdated;
		}
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
