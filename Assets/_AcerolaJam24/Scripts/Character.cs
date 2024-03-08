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
using TMPro;
using UnityEngine.Animations.Rigging;

public class Character : MonoBehaviour
{
    [SerializeField] private Transform _characterRoot;
    [SerializeField] private bool _lookAtPlayer;
    [SerializeField] private float _blendActingTime = 0.5f;
    [SerializeField] private float _mouthShutThreshold = 0.1f;
    [SerializeField] private float _sentencePauseTime = 0.5f;
    [SerializeField] private float _backFromInterruptionTime = 1.0f;
    [SerializeField] private float _isQuietTime = 0.1f;

    private class RestoreInfo
    {
        public ActingClip _actingClip;
        public AudioClipDef _audioClip;
        public float _lastSentenceTime;
        public int _lastSentenceClipFrame;
    }

    private CharacterDef _characterDef;

    private ActingClip _currentActingClip;
    private int _actingClipFrame;
    private AudioClipDef _currAudioClip;

    private ActingClip _blendActingClip;
    private int _blendClipFrame;

    private float _blendClipAmount = 0.0f;

    private bool _playingActingClip;

    private Transform _characterModel;
    private RigBuilder _rigBuilder;

    List<Transform> _actingClipTargets = new List<Transform>();
    AudioSource _audio;

    Rig _IKRig;
    float _blendActing = 0.0f;
    bool _blendingIn = true;
    Vector3 _startingPos;
    CharacterMan _characterMan;

    bool _pendingPersonalSpaceBreach = false;
    bool _personalSpaceBreached = false;
    float _personalSpaceTimer = 0.0f;
    CharacterDef.Interruptable _personalSpaceInterruptable;
    float _interruptionTimer = 0.0f;

    bool _hasRestorableClip = false;
    RestoreInfo _restoreInfo;

    FaceController _face;
    float _mouthShutTimer = 0.0f;
    float _lastSentenceTime = 0.0f;
    int _lastSentenceClipFrame = 0;

    public CharacterDef GetCharacterDef() { return _characterDef; }

    public bool IsCurrentlyQuiet()
    {
        if (_mouthShutTimer >= _isQuietTime)
            return true;

        return false;
    }

    public void StopCharacter()
    {
        _audio.Stop();
        _face.StopClip();
    }

    public void PersonalSpaceBreached(bool onOff)
    {
        _pendingPersonalSpaceBreach = onOff;
    }

    public void SetCharacterModel(Transform model)
    {
        _characterModel = model;
        _rigBuilder = _characterModel.GetComponent<RigBuilder>();
        _IKRig = _characterModel.GetComponent<Rig>();
        _audio = _characterModel.GetComponent<AudioSource>();
        _face = _characterModel.GetComponent<FaceController>();

        // Tell the personal space object about the character
        PersonalSpace personalSpace = _characterModel.GetComponent<PersonalSpace>();
        if (personalSpace)
        {
            CharacterDef def = _characterDef;
            _personalSpaceInterruptable = def.GetInterruptable(CharacterDef.Interruptable.Type.PersonalSpaceBreach);
            if (_personalSpaceInterruptable != null)
                _personalSpaceTimer = _personalSpaceInterruptable._startTime;

            personalSpace.SetCharacter(this);
        }

        if (_rigBuilder)
            _rigBuilder.Build();
    }

    public Transform GetCharacterRoot()
    {
        return _characterRoot;
    }

    public void Setup(CharacterMan characterMan, CharacterDef characterDef)
    {
        _characterDef = characterDef;
        _characterMan = characterMan;
    }
  
    public void PlayInterruption(CharacterDef.Interruptable.InterruptableClip interruptableClip)
    {
        // Are we already playing an interruption, if so, ignore this one...
        if (_interruptionTimer > 0.0f)
            return;

        // Kick off the interruption
        StoreCurrentClip();
        PlayActingClip(interruptableClip._actingClip, interruptableClip._audioClip);
        _interruptionTimer = interruptableClip._audioClip._audioClip.length + _backFromInterruptionTime;
    }

    public void PlayActingClip(ActingClip actingClip, AudioClipDef audioClip)
    {
        if (_currentActingClip == null)
        {
            // Blend from nothing
            _blendActing = 1.0f;
            _blendingIn = true;

            _currentActingClip = actingClip;
            _actingClipFrame = 0;
        }
        else
        {
            // We are blending from the current clip
            _blendActingClip = actingClip;
            _blendClipFrame = 0;
            _blendClipAmount = 1.0f;
        }

        _playingActingClip = true;
        _lastSentenceClipFrame = 0;
        _lastSentenceTime = 0.0f;

        _currAudioClip = audioClip;
        actingClip.SetAudioClip(audioClip._audioClip);
        _face.PlayClip(audioClip);

        if (audioClip != null)
        {
            _audio.clip = audioClip._audioClip;
            _audio.time = 0.0f;
            _audio.Play();
        }
        else
        {
            // We don't have any audio to play
            _audio.Stop();
            _face.StopClip();
        }

        // Find the clip targets
        // <TODO> There's probably a more optimal way to do this...
        if (_rigBuilder)
            _rigBuilder.Build();

        _actingClipTargets.Clear();
        foreach (ActingClip.ObjData objData in actingClip._objData)
        {
            foreach (Transform trans in _characterModel.GetComponentsInChildren<Transform>())
            {
                if (trans.name == objData._boneName)
                {
                    _actingClipTargets.Add(trans);
                    break;
                }
            }
        }
    }

    void StoreCurrentClip()
    {
        if (_restoreInfo == null)
            _restoreInfo = new RestoreInfo();

        if (_playingActingClip == true)
        {
            _restoreInfo._actingClip = _currentActingClip;
            _restoreInfo._audioClip = _currAudioClip;
            _restoreInfo._lastSentenceTime = _lastSentenceTime;
            _restoreInfo._lastSentenceClipFrame = _lastSentenceClipFrame;

            _hasRestorableClip = true;
        }
    }

    void RestoreClip()
    {
        if (_hasRestorableClip)
        {
            // Kick off the restored clip
            PlayActingClip(_restoreInfo._actingClip, _restoreInfo._audioClip);

            // Set the frames etc.. to being where it was last played
            if (_restoreInfo != null)
            {
                if (_audio.clip != null)
                    _audio.time = Mathf.Min(_restoreInfo._lastSentenceTime, _audio.clip.length - 0.01f);

                _blendClipFrame = _restoreInfo._lastSentenceClipFrame;
            }

            // The clip has been restored, we shouldn't be restoring it again until we store another one
            _hasRestorableClip = false;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (_playingActingClip)
        {
            int index = 0;

            // Blending clips
            if ((_blendClipAmount > 0.0f) && (_blendActingClip != null))
            {
                _blendClipAmount -= Time.fixedDeltaTime / _blendActingTime;
                if (_blendClipAmount <= 0.0f)
                {
                    // We've finished blending, so swap the clip sources
                    _currentActingClip = _blendActingClip;
                    _actingClipFrame = _blendClipFrame;

                    _blendClipAmount = 0.0f;
                    _blendActingClip = null;
                    _blendClipFrame = 0;
                }
                else
                {
                    // Lerp between the current and blend clips
                    foreach (Transform target in _actingClipTargets)
                    {
                        ActingClip.ObjData currObjData = _currentActingClip._objData[index];
                        ActingClip.ObjData blendObjData = _blendActingClip._objData[index];

                        if (currObjData._applyPos)
                        {
                            Vector3 currPos = currObjData._posFrames[_actingClipFrame];
                            Vector3 blendPos = blendObjData._posFrames[_blendClipFrame];
                            Vector3 targetPos = Vector3.Lerp(blendPos, currPos, _blendClipAmount);

                            PlayMovement(target, targetPos);
                        }

                        if (currObjData._applyRot)
                        {
                            Quaternion currRot = currObjData._rotFrames[_actingClipFrame];
                            Quaternion blendRot = blendObjData._rotFrames[_blendClipFrame];
                            Quaternion targetRot = Quaternion.Lerp(blendRot, currRot, _blendClipAmount);

                            PlayRotation(target, currObjData, targetRot);
                        }

                        index++;
                    }

                    if (_blendClipFrame < _blendActingClip._numFrames)
                        _blendClipFrame++;
                }
            }

            if (_blendClipAmount <= 0.0f)
            {
                foreach (Transform target in _actingClipTargets)
                {
                    ActingClip.ObjData objData = _currentActingClip._objData[index];

                    if (objData._applyPos)
                        PlayMovement(target, objData._posFrames[_actingClipFrame]);

                    if (objData._applyRot)
                        PlayRotation(target, objData, objData._rotFrames[_actingClipFrame]);

                    index++;
                }

                if (_actingClipFrame < _currentActingClip._numFrames)
                    _actingClipFrame++;

                // Have we finished the animation?
                if (_actingClipFrame >= _currentActingClip._numFrames)
                {
                    // If the clip looping?
                    if (_currentActingClip._looped)
                    {
                        _actingClipFrame = _currentActingClip._numFrames - 1;
                        _blendActingClip = _currentActingClip;
                        _blendClipFrame = 0;
                        _blendClipAmount = 1.0f;
                    }
                    else
                    {
                        _blendActing = 1.0f;
                        _blendingIn = false;
                        _playingActingClip = false;
                    }
                }
            }
        }

        // For blending from and to no-clip
        if (_blendActing > 0.0f)
        {
            _blendActing -= Time.fixedDeltaTime / _blendActingTime;
            if (_blendActing <= 0.0f)
                _blendActing = 0.0f;

            if (_IKRig != null)
            {
                if (_blendingIn)
                    _IKRig.weight = 1.0f - _blendActing;
                else
                    _IKRig.weight = _blendActing;
            }
        }

        UpdatePersonalSpace();
        UpdateInterruption();
        UpdateMouth();
        UpdateLookAt();
    }

    void UpdatePersonalSpace()
    {
        // Don't apply the personal space if currently blending
        if (_blendClipAmount > 0.0f)
            return;

        if (_personalSpaceInterruptable == null)
            return;

        if (_pendingPersonalSpaceBreach)
        {
            if (_personalSpaceBreached == false)
            {
                // Wait until the timer has ran out before the breach kicks in
                if (_personalSpaceTimer > 0.0f)
                {
                    _personalSpaceTimer -= Time.fixedDeltaTime;
                    return;
                }

                
                // Start the personal space breach and store off the current action
                CharacterDef def = _characterDef;

                if (_personalSpaceInterruptable != null)
                {
                    CharacterDef.Interruptable.InterruptableClip clip = _personalSpaceInterruptable._clips[0];

                    // Kick off the interruption
                    StoreCurrentClip();
                    PlayActingClip(clip._actingClip, clip._audioClip);
                    _personalSpaceTimer = clip._minTime;
                }
                _personalSpaceBreached = _pendingPersonalSpaceBreach;
            }
        }
        else
        {
            if (_personalSpaceBreached == true)
            {
                // Only stop it after a certain amount of time
                if (_personalSpaceTimer > 0.0f)
                {
                    _personalSpaceTimer -= Time.fixedDeltaTime;
                    return;
                }

                // Stop the personal space breach and restore the previous action

                // Resume back to what we were doing
                RestoreClip();
                _personalSpaceBreached = _pendingPersonalSpaceBreach;
                _personalSpaceTimer = _personalSpaceInterruptable._startTime;
            }
        }
    }

    void UpdateInterruption()
    {
        if (_interruptionTimer >= 0.0f)
        {
            _interruptionTimer -= Time.deltaTime;
            if (_interruptionTimer <= 0.0f)
            {
                RestoreClip();
            }
        }
    }

    void UpdateMouth()
    {
        // When the mouth shuts for a time, mark where this was and record the time from the audio as a sentence
        float mouthOpenAmount = _face.GetSpeechVolume();

        if (mouthOpenAmount <= _mouthShutThreshold)
            _mouthShutTimer += Time.fixedDeltaTime;
        else
            _mouthShutTimer = 0.0f;

        if (_mouthShutTimer >= _sentencePauseTime)
        {
            _lastSentenceTime = _audio.time - _sentencePauseTime;

            int pauseTimeToFrames = Mathf.FloorToInt(_sentencePauseTime / Time.fixedDeltaTime);
            _lastSentenceClipFrame = _actingClipFrame - pauseTimeToFrames;
            if (_lastSentenceClipFrame < 0)
                _lastSentenceClipFrame = 0;

            _mouthShutTimer = 0.0f;
        }
    }

    void UpdateLookAt()
    {
        if (_lookAtPlayer)
        {
            Transform player = _characterMan.GetPlayer();
            if (player)
            {
                Vector3 lookAtPos = player.position;
                lookAtPos.y = transform.position.y;
                transform.LookAt(lookAtPos);
            }
        }
    }

    void PlayMovement(Transform target, Vector3 pos)
    {
        target.localPosition = pos;
    }

    void PlayRotation(Transform target, ActingClip.ObjData obj, Quaternion rot)
    {
        target.localRotation = rot;
    }
}

