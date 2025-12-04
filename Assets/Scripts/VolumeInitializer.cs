using UnityEngine;
using UnityEngine.Audio;

public class VolumeInitializer : MonoBehaviour
{
    [Header("必须把 AudioMixer 拖进来")]
    public AudioMixer mainAudioMixer;

    void Start()
    {
        float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 0.65f);
        float musicDb = Mathf.Log10(Mathf.Clamp(savedMusic, 0.0001f, 1f)) * 20;
        mainAudioMixer.SetFloat("MusicVol", musicDb);
        float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 0.75f);
        float sfxDb = Mathf.Log10(Mathf.Clamp(savedSFX, 0.0001f, 1f)) * 20;
        mainAudioMixer.SetFloat("SFXVol", sfxDb);
    }
}