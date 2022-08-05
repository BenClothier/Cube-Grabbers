using Unity.Netcode;
using UnityEngine;
using System.Collections;

namespace Game.Utility.Networking
{
    public static class NetworkingTools
    {
        /// <summary>
        /// Coroutine function that will despawn an object after a certain number of seconds. MUST BE CALLED WITH 'StartCoroutine()'.
        /// </summary>
        /// <param name="netObj">The network object to despawn.</param>
        /// <param name="seconds">The number of seconds to wait before despawning.</param>
        /// <returns>Unscaled time to wait for.</returns>
        public static IEnumerator DespawnAfterSeconds(NetworkObject netObj, float seconds = 1)
        {
            yield return new WaitForSecondsRealtime(seconds);
            netObj.Despawn();
        }
    }
}
