using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SimpleFootstep : MonoBehaviour
{
    [Header("audio")]
    public AudioClip[] footstepClips;
    public AudioClip[] landingClips;
    [Range(0f, 1f)] public float footstepVolume = 0.7f;
    [Range(0f, 1f)] public float landingVolume = 1f;

    AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1f; // 3D sound (optional)
    }

    // called by animation event "OnFootstep"
    void OnFootstep(AnimationEvent animationEvent)
    {
        // if this layer isn’t actually playing the clip, ignore
        if (animationEvent.animatorClipInfo.weight < 0.5f)
            return;

        if (footstepClips == null || footstepClips.Length == 0)
            return;

        var clip = footstepClips[Random.Range(0, footstepClips.Length)];
        source.PlayOneShot(clip, footstepVolume);
    }

    // called by animation event "OnLand" (some StarterAssets clips use this)
    void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight < 0.5f)
            return;

        if (landingClips == null || landingClips.Length == 0)
            return;

        var clip = landingClips[Random.Range(0, landingClips.Length)];
        source.PlayOneShot(clip, landingVolume);
    }
}
