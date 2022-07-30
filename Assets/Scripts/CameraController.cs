using UnityEngine;
using Unity.Netcode;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("Hello");
        FindObjectOfType<NetworkManager>().OnClientConnectedCallback += FindCameraTargets;
    }

    private void FindCameraTargets(ulong id)
    {
        Debug.Log("Hello");
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        CinemachineTargetGroup.Target[] targets = new CinemachineTargetGroup.Target[players.Length];
        for (int i = 0; i < players.Length; i++)
        {
            CinemachineTargetGroup.Target target = new CinemachineTargetGroup.Target();
            target.target = players[i].transform;
            target.weight = 1;
            target.radius = 2;
            targets[i] = target;
        }
        GetComponent<CinemachineTargetGroup>().m_Targets = targets;
    }
}
