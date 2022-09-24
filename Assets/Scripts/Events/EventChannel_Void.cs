using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Void Event Channel")]
public class EventChannel_Void : ScriptableObject
{
    public UnityAction OnEventInvocation;

    public void InvokeEvent()
    {
        OnEventInvocation?.Invoke();
    }
}
