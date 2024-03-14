using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicMan : MonoBehaviour
{
    [SerializeField] private AudioClip _backgroundAmbience;
    [SerializeField] private AudioClip _actionMusic;
    [SerializeField] private float _ambienceVolume = 0.6f;
    [SerializeField] private float _musicVolume = 0.1f;

    private AudioSource _audio;

    public void StartActionMusic()
    {
        PlayAudio(_actionMusic, _musicVolume);
    }

    public void StopMusic()
    {
        PlayAudio(_backgroundAmbience, _ambienceVolume);
    }

    public void StopAll()
    {
        _audio.Stop();
    }

    void PlayAudio(AudioClip clip, float volume)
    {
        _audio.Stop();
        _audio.clip = clip;
        _audio.Play();
        _audio.volume = volume;
    }

    // Start is called before the first frame update
    void Start()
    {
        _audio = GetComponent<AudioSource>();
        _audio.clip = _backgroundAmbience;
        _audio.Play();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
