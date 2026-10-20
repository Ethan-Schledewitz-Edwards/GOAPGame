using UnityEngine;
using UnityEngine.InputSystem;

public class TestPostWwiseEvent : MonoBehaviour
{
    [SerializeField] private AK.Wwise.Event _atkEvent;
    [SerializeField] private AK.Wwise.Event _hitEvent;
    [SerializeField] private AK.Wwise.Event _musPlayEvent;
    [SerializeField] private AK.Wwise.Event _musResumeEvent;
    [SerializeField] private AK.Wwise.Event _musPauseEvent;
    
    private void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame) _atkEvent.Post(gameObject);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) _hitEvent.Post(gameObject);
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            _musPlayEvent.Post(gameObject);
        }
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            _musPauseEvent.Post(gameObject);
        }
    }
}
