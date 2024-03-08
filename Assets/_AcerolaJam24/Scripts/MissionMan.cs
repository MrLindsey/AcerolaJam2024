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

public class MissionMan : MonoBehaviour
{
    [SerializeField] float _startDelay = 3.0f;
    [SerializeField] MissionDef _mission;
    [SerializeField] CharacterMan _characterMan;

    private TaskMan _taskMan;
    private int _curCharacterIndex = 0;

    public int GetCharacterIndex() { return _curCharacterIndex; }

    public void RestartMission()
    {
        _curCharacterIndex = 0;
        StartMission();
    }

    public void CharacterFinished()
    {
        Character character = _characterMan.GetCurrentCharacter();
        character.StopCharacter();

        // Start the next character
        _curCharacterIndex++;
        if (_curCharacterIndex < _mission._characterDefs.Length)
        {
            Invoke(nameof(StartCharacter), _startDelay);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _taskMan = GetComponent<TaskMan>();
        Invoke(nameof(StartMission), _startDelay);
    }

    private void StartMission()
    {
        StartCharacter();
    }

    private void StartCharacter()
    {
        CharacterDef characterDef = _mission._characterDefs[_curCharacterIndex];
        _characterMan.StartCharacter(characterDef, _taskMan);
    }
}
