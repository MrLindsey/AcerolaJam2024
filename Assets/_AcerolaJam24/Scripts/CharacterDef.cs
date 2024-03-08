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

[System.Serializable]
public class CharacterDef
{
    [System.Serializable]
    public class Interruptable
    {
        public enum Type
        {
            None,
            PersonalSpaceBreach,
        }

        [System.Serializable]
        public class InterruptableClip
        {
            public float _minTime = 3.0f;
            public AudioClipDef _audioClip;
            public ActingClip _actingClip;
        }

        public string _name;
        public Type _type;
        public float _startTime = 1.0f;
        public InterruptableClip[] _clips;
    }

    public enum EnterType
    {
        None,               
        WalkToSpawnPoint
    }

    public enum ExitType
    {
        None,               
        BackToSpawnPoint,   
    }

    public string _name;
    public Transform _characterPrefab;
    public EnterType _enterType = EnterType.None;
    public ExitType _exitType = ExitType.None;
    public bool _useExistingCharacter;
    public string _characterNameInScene;
    public bool _rotateOnExit = true;
    public TaskListDef[] _taskLists;

    public Interruptable[] _commonInterruptables;

    public Interruptable GetInterruptable(Interruptable.Type type)
    {
        foreach (Interruptable interruptable in _commonInterruptables)
        {
            if (interruptable._type == type)
                return interruptable;
        }

        return null;
    }

}
