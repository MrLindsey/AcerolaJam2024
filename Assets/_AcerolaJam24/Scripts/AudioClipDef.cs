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

[CreateAssetMenu]
public class AudioClipDef : ScriptableObject
{
    public AudioClip _audioClip;
    public int _fixedUpdateRate = 2;
    public List<float> _volumeData = new List<float>();

}
