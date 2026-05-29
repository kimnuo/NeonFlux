using UnityEngine;

public class EngineAudioController : MonoBehaviour
{
    [SerializeField] private Rigidbody targetRigidbody;
    [SerializeField] private AudioSource engineAudio;
    [SerializeField] private AudioSource skidAudio;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private AudioClip maxRpmClip;
    [SerializeField] private float minSpeed = 1f;
    [SerializeField] private float driftAngleThreshold = 20f;
    [SerializeField] private float volumeMultiplier = 0.7f;

    private float _baseVolume = 1f;
    private float _skidBaseVolume = 1f;
    private float _initialYaw;
    private AudioClip _medOnClip;

    private void Awake()
    {
        if (engineAudio == null) engineAudio = GetComponent<AudioSource>();
        if (targetRigidbody == null) targetRigidbody = GetComponent<Rigidbody>();
        if (playerController == null) playerController = GetComponent<PlayerController>();

        if (skidAudio == null && engineAudio != null)
        {
            AudioSource[] sources = GetComponents<AudioSource>();
            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != engineAudio)
                {
                    skidAudio = sources[i];
                    break;
                }
            }
        }

        _initialYaw = transform.eulerAngles.y;

        if (engineAudio != null)
        {
            _baseVolume = engineAudio.volume;
            engineAudio.volume = _baseVolume * Mathf.Clamp01(volumeMultiplier);
            _medOnClip = engineAudio.clip;
            if (engineAudio.isPlaying) engineAudio.Pause();
        }

        if (skidAudio != null)
        {
            _skidBaseVolume = skidAudio.volume;
            skidAudio.volume = _skidBaseVolume * Mathf.Clamp01(volumeMultiplier);
            if (skidAudio.isPlaying) skidAudio.Pause();
        }
    }

    private void FixedUpdate()
    {
        if (engineAudio == null || targetRigidbody == null) return;

        float speed = targetRigidbody.velocity.magnitude;
        bool isMoving = speed >= minSpeed;
        if (!isMoving)
        {
            if (engineAudio.isPlaying) engineAudio.Pause();
            if (skidAudio != null && skidAudio.isPlaying) skidAudio.Pause();
            return;
        }

        bool driftActive = IsDriftSmokeActive();
        bool highAngle = driftActive && GetYawDelta() >= driftAngleThreshold;
        AudioClip targetClip = (highAngle && maxRpmClip != null) ? maxRpmClip : _medOnClip;

        if (engineAudio.clip != targetClip)
        {
            engineAudio.clip = targetClip;
            engineAudio.Play();
        }
        else if (!engineAudio.isPlaying)
        {
            engineAudio.Play();
        }

        if (skidAudio != null)
        {
            if (driftActive)
            {
                if (!skidAudio.isPlaying) skidAudio.Play();
            }
            else if (skidAudio.isPlaying)
            {
                skidAudio.Pause();
            }
        }

        engineAudio.volume = _baseVolume * Mathf.Clamp01(volumeMultiplier);
        if (skidAudio != null) skidAudio.volume = _skidBaseVolume * Mathf.Clamp01(volumeMultiplier);
    }

    private float GetYawDelta()
    {
        return Mathf.Abs(Mathf.DeltaAngle(_initialYaw, transform.eulerAngles.y));
    }

    private bool IsDriftSmokeActive()
    {
        if (playerController == null) return false;

        return IsParticleActive(playerController.driftSmokeParticle)
            || IsParticleActive(playerController.driftSmokeLeftParticle)
            || IsParticleActive(playerController.driftSmokeRightParticle);
    }

    private static bool IsParticleActive(ParticleSystem particleSystem)
    {
        return particleSystem != null && (particleSystem.isEmitting || particleSystem.isPlaying);
    }
}
