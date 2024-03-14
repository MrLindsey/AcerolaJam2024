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

public class SfxPlayer : MonoBehaviour
{
    public enum SfxTypes
    {
        PowerDown,
        BreakButton,
    }

    [SerializeField] private SfxTypes _types;
    [SerializeField] private AudioClip[] _audioClips;
    [SerializeField] private float[] _audioVolumes;

    private AudioSource _audio;

    public void PlayClip(SfxTypes type)
    {
        int index = (int)type;
        _audio.clip = _audioClips[index];
        _audio.volume = _audioVolumes[index];
        _audio.Play();
    }

    // Start is called before the first frame update
    void Awake()
    {
        _audio = GetComponent<AudioSource>();
    }

}
