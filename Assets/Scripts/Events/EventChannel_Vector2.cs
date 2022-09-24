using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Vector2 Event Channel")]
public class EventChannel_Vector2 : ScriptableObject
{
    public UnityAction<Vector2> OnEventInvocation;

    public void InvokeEvent(Vector2 vec2)
    {
        OnEventInvocation?.Invoke(vec2);
    }
}
