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
public class ActingClip : ScriptableObject
{
    [System.Serializable]
    public class ObjData
    {
        public string _name;
        public string _boneName;
        public bool _applyPos;
        public bool _applyRot;
        public Quaternion _rotScale;
        public Quaternion _rotOffset;

        // Frame data
        public List<Vector3> _posFrames = new List<Vector3>();
        public List<Quaternion> _rotFrames = new List<Quaternion>();

        public List<float> _leftTriggerFrames = new List<float>();
        public List<float> _rightTriggerFrames = new List<float>();

        public List<float> _leftGrabFrames = new List<float>();
        public List<float> _rightGrabFrames = new List<float>();

        public void ResetFrameData()
        {
            _posFrames.Clear();
            _rotFrames.Clear();

            _leftTriggerFrames.Clear();
            _rightTriggerFrames.Clear();

            _leftGrabFrames.Clear();
            _rightGrabFrames.Clear();
        }

        public void CopyFrameData(ObjData src)
        {
            _posFrames = new List<Vector3>(src._posFrames);
            _rotFrames = new List<Quaternion>(src._rotFrames);

            _leftTriggerFrames = new List<float>(src._leftTriggerFrames);
            _rightTriggerFrames = new List<float>(src._rightTriggerFrames);

            _leftGrabFrames = new List<float>(src._leftGrabFrames);
            _rightGrabFrames = new List<float>(src._rightGrabFrames);
        }
    }

    public float _overrideTime = 0.0f;
    public int _numFrames = 0;
    public bool _applyHead = true;
    public bool _applyArms = true;
    public bool _looped = false;
    public List<ObjData> _objData = new List<ObjData>();

    private AudioClip _audioClip;

    public void SetAudioClip(AudioClip clip) { _audioClip = clip; }
    public AudioClip GetAudioClip() { return _audioClip; }

}

