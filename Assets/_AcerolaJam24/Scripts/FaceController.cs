//============================================================
// For Acerola Game Jam 2024
// --------------------------
// Copyright (c) 2024 Ian Lindsey
// This code is licensed under the MIT license.
// See the LICENSE file for details.
//============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FaceController : MonoBehaviour
{
    [SerializeField] private Transform _mouth;
    [SerializeField] private Transform _eyes;
    [SerializeField] private AudioClipDef _audioClipDef;
    
    [SerializeField] private float _volumeMult = 10.0f;
    [SerializeField] private float _mouthSpeed = 5.0f;
    [SerializeField] private float _quietTime = 0.5f;

    [SerializeField] private float _blinkSpeed = 20.0f;
    [SerializeField] private float _blinkTime = 3.0f;
    [SerializeField] private float _blinkRandomness = 2.0f;
    [SerializeField] private float _blinkShutTime = 0.2f;

    private float _volumeValue;
    private float _targetVolumeValue;
    private float _lastQuietTime;
    private int _audioCheckFrame;
    private int _audioCheckFrequency;
    private int _audioFrameCount;
    private float _quietTimer;

    private AudioSource _audio;
    private Vector3 _originalMouthScale;
    private Vector3 _originalEyeScale;

    private float _currBlink;
    private float _blinkTarget;
    private float _blinkTimer;
    private bool _isBlinking;

    public void PlayClip(AudioClipDef audioClipDef)
    {
        _audio.Stop();
        _audioClipDef = audioClipDef;
        PlayAudio();
    }

    public void StopClip()
    {
        _audio.Stop();
        _audioClipDef = null;
        _targetVolumeValue = 0.0f;
    }

    public float GetSpeechVolume() { return _targetVolumeValue; }

    // Start is called before the first frame update
    void Awake()
    {
        _audio = GetComponent<AudioSource>();
        _originalMouthScale = _mouth.localScale;
        _mouth.localScale = new Vector3(_originalMouthScale.x, 0.0f, _originalMouthScale.z);

        // Setup the eyes
        _originalEyeScale = _eyes.localScale;
        CreateNextBlink();
    }

    void PlayAudio()
    {
        _audioFrameCount = 0;
        _audio.clip = _audioClipDef._audioClip;
        _audioCheckFrequency = _audioClipDef._fixedUpdateRate;
        _audio.Play();
    }

    void CreateNextBlink()
    {
        float halfRandomness = _blinkRandomness * 0.5f;
        _blinkTimer = _blinkTime + Random.Range(-halfRandomness, halfRandomness);
        _blinkTarget = 1.0f;

        _isBlinking = false;
    }

    void GetAudioVolume()
    {
        if (_audio.isPlaying)
        {
            _targetVolumeValue = _audioClipDef._volumeData[_audioFrameCount] * _volumeMult;
            _audioFrameCount++;

            // Has the audio looped?
            if (_audioFrameCount >= _audioClipDef._volumeData.Count)
                _audioFrameCount = 0;
        }
        else
        {
            _targetVolumeValue = 0.0f;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float timeScalar = Time.fixedDeltaTime;

        // Blink logic
        if (_blinkTimer > 0.0f)
        {
            _blinkTimer -= timeScalar;
            if (_isBlinking)
            {
                if (_blinkTimer <= 0.0f)
                    CreateNextBlink();
            }
            else
            {
                if (_blinkTimer <= 0.0f)
                {
                    _blinkTarget = 0.0f;
                    _isBlinking = true;
                    _blinkTimer = _blinkShutTime;
                }
            }

            _currBlink = Mathf.Lerp(_currBlink, _blinkTarget, _blinkSpeed * timeScalar);
            Vector3 scale = _originalEyeScale;
            scale.y *= _currBlink;
            _eyes.localScale = scale;
        }

        // Lerp towards the target RMS value
        _volumeValue = Mathf.Lerp(_volumeValue, _targetVolumeValue, _mouthSpeed * timeScalar);

        // Get the volume samples once every 'n' frames
        if (_audio.isPlaying)
        {
            _audioCheckFrame--;
            if (_audioCheckFrame <= 0)
            {
                GetAudioVolume();

                // Scale the mouth to the sample's rms value
                Vector3 scale = _originalMouthScale;
                scale.y *= _volumeValue;
                _mouth.localScale = scale;

                _audioCheckFrame = _audioCheckFrequency;
            }
        }
        else
        {
            _targetVolumeValue = 0.0f;
        }

        // Check for quiet pause in the audio in case we need to go back to it
        if (_volumeValue < 0.05f)
        {
            _quietTimer += timeScalar;
            if (_quietTimer > _quietTime)
            {
                _lastQuietTime = _audio.time;
                _quietTimer = 0.0f;
            }
        }
        else
        {
            _quietTimer = 0.0f;
        }
    }
}
