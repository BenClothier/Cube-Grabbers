namespace Game.Behaviours.VisualEffects
{
    using Cinemachine;
    using UnityEngine;
    using System.Collections;

    public class CameraShake : MonoBehaviour
    {
        [SerializeField] private AnimationCurve amplitudeOverTime;
        [SerializeField] private AnimationCurve frequencyOverTime;
        [Space]
        [SerializeField] private EventChannel_Void startChannel;
        [SerializeField] private EventChannel_Void stopChannel;

        private CinemachineVirtualCamera vCam;
        private CinemachineBasicMultiChannelPerlin vCamNoise;

        private float startTime;

        private void OnEnable()
        {
            if ((vCam = GetComponentInChildren<CinemachineVirtualCamera>()) is not null && (vCamNoise = vCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>()) is not null && amplitudeOverTime is not null && frequencyOverTime is not null)
            {
                startChannel.OnEventInvocation += StartShake;
                stopChannel.OnEventInvocation += StopShake;
            }
            else
            {
                Debug.LogWarning("Could not find virtual camera, could not find noise component, or some shake information was missing. This camera shake will not function.");
            }
        }

        private void StartShake()
        {
            StartCoroutine(ShakeCamera());
        }

        private void StopShake()
        {
            StopAllCoroutines();
            vCamNoise.m_AmplitudeGain = 0;
            vCamNoise.m_FrequencyGain = 0;
        }

        private IEnumerator ShakeCamera()
        {
            startTime = Time.time;

            while (true)
            {
                vCamNoise.m_AmplitudeGain = amplitudeOverTime.Evaluate(Time.time - startTime);
                vCamNoise.m_FrequencyGain = frequencyOverTime.Evaluate(Time.time - startTime);
                yield return null;
            }
        }
    }
}