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

public class GrabbableObjectController : MonoBehaviour
{
    [SerializeField] bool _isGrabbable = true;
    [SerializeField] bool _playAnimOnGrab = false;
    [SerializeField] bool _oneShotAnim = true;

    private AudioSource _audio;
    private Animation _anim;

    private bool _hasPlayedAnim;

    public bool OnGrabObject()
    {
        if (_playAnimOnGrab)
        {
            if (_anim && !_hasPlayedAnim)
            {
                _anim.Play();
                if (_oneShotAnim)
                    _hasPlayedAnim = true;
            }

            if (_audio)
            {
                _audio.Stop();
                _audio.Play();
            }
        }

        return _isGrabbable;
    }

    // Start is called before the first frame update
    void Awake()
    {
        _anim = GetComponent<Animation>();
        _audio = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
