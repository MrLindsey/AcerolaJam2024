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
public class TaskListDef : ScriptableObject
{
    [System.Serializable]
    public class Chat
    {
        public string _chatString;
        public AudioClipDef _audioClip;
        public ActingClip _actingClip;
        public bool _interruptible = false;
        public float _chatTime = 0.0f;
        public string _grabObjectName;
    }

    public string _title;
    public Chat[] _startChat;
    public Chat[] _completeChat;

    public TaskDef[] _tasks;
}
