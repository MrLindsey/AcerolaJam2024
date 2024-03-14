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

public class CharacterMan : MonoBehaviour
{
    public enum State
    {
        Invalid,
        None,
        Walking,
        Interacting,
        Exiting,
    }

    [SerializeField] private Transform _player;
    [SerializeField] private Transform _baseCharacterPrefab;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private Transform _interactPoint;
    [SerializeField] private MissionMan _missionMan;
    [SerializeField] private Character[] _charactersInScene;
    [SerializeField] ActingClip _walkingClip;

    [SerializeField] float _walkSpeed = 2.0f;
    [SerializeField] float _rotSpeed = 20.0f;

    private Character _character;
    private State _state = State.Invalid;
    private CharacterDef _characterDef;
    private TaskListDef _curTaskList;
    private TaskMan _taskMan;

    private List<TaskListDef.Chat> _currentChats = new List<TaskListDef.Chat>();
    private TaskListDef.Chat _currentChat;
    private int _currentChatIndex = 0;
    private int _currentTaskListIndex = 0;
    private float _chatTimer = 0.0f;
    private bool _pendingNextChat = false;

    private List<Character> _characterList = new List<Character>();

    public Transform GetPlayer() { return _player; }

    public Character GetCurrentCharacter() { return _character; }

    public CharacterDef GetCurrentCharacterDef() { return _characterDef; }

    public void StartCharacter(CharacterDef characterDef, TaskMan taskMan)
    {
        if (characterDef._useExistingCharacter)
        {
            bool found = false;

            // Find the existing character in the list
            foreach (Character character in _characterList)
            {
                CharacterDef listCharacterDef = character.GetCharacterDef();
                if (listCharacterDef._characterPrefab == characterDef._characterPrefab)
                {
                    _character = character;
                    _characterDef = characterDef;
                    _character.Setup(this, characterDef);

                    found = true;
                    StartInteracting();
                    break;
                }
            }

            if (found == false)
                Debug.Log("Cannot find existing character: " + characterDef._name);
        }
        else if (characterDef._characterPrefab == null)
        {
            // The character is already in the scene, therefore use that character
            bool found = false;

            // Look for the character name in characters in scene list
            foreach (Character sceneCharacter in _charactersInScene)
            {
                if (characterDef._characterNameInScene == sceneCharacter.name)
                {
                    found = true;
                    _character = sceneCharacter;
                    _characterDef = characterDef;

                    _character.Setup(this, characterDef);
                    _character.SetCharacterModel(_character.transform);
                    _character.gameObject.SetActive(true);

                    StartInteracting();
                    break;
                }
            }

            if (found == false)
                Debug.Log("Cannot find character in the scene: " + characterDef._name);
        }
        else
        {
            int characterIndex = _missionMan.GetCharacterIndex();
            Transform spawnPoint = _spawnPoints[characterIndex];

            // Create the character in the scene
            Transform newCharacter = Instantiate(_baseCharacterPrefab, spawnPoint.position, spawnPoint.rotation, transform);
            _character = newCharacter.GetComponent<Character>();

            _taskMan = taskMan;
            _characterDef = characterDef;

            _character.transform.position = spawnPoint.position;
            _character.transform.localRotation = spawnPoint.localRotation;
            _character.Setup(this, _characterDef);

            Transform characterRoot = _character.GetCharacterRoot();
            Transform newModel = Instantiate(characterDef._characterPrefab, characterRoot.position, characterRoot.rotation, newCharacter);

            _character.SetCharacterModel(newModel);

            if (_characterDef._enterType == CharacterDef.EnterType.WalkToSpawnPoint)
            {
                _state = State.Walking;
                if (_walkingClip)
                    _character.PlayActingClip(_walkingClip, null);
            }
            else
            {
                StartInteracting();
            }
            _characterList.Add(_character);
        }
    }

    public void TakeObject(Transform takeObject)
    {
       // _taskMan.TakeObject(takeObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        // Disable the characters in the scene until we're ready to use them
        foreach (Character character in _charactersInScene)
            character.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        switch (_state)
        {
            case State.Walking:
                UpdateWalking();
                break;

            case State.Exiting:
                UpdateExiting();
                break;

#if UNITY_EDITOR
            // For debugging
            case State.Interacting:
                UpdateDebug();
                break;
#endif
        }

        if (_pendingNextChat)
        {
            if (_character.IsCurrentlyQuiet())
            {
                _pendingNextChat = false;
                OnShowNextChat();
            }
        }
        else
        {
            if ((_currentChatIndex >= 0) && (_currentChatIndex < _currentChats.Count))
            {
                if (_chatTimer > 0.0f)
                {
                    _chatTimer -= Time.deltaTime;
                    if (_chatTimer <= 0.0f)
                    {
                        _pendingNextChat = true;
                        _chatTimer = 0.0f;
                    }
                }
            }
        }
    }

    void UpdateDebug()
    {
        // Pressed C for complete current Task
        if (Input.GetKeyUp(KeyCode.C))
        {
            CompleteTaskList();
        }
    }

    void UpdateWalking()
    {
        Vector3 dir = _interactPoint.position - _character.transform.position;
        dir.Normalize();

        _character.transform.position += (dir * _walkSpeed) * Time.deltaTime;
        if (_character.transform.position.z < _interactPoint.position.z)
        {
            // Reached the destination
            Vector3 pos = _character.transform.position;
            pos.z = _interactPoint.position.z;
            _character.transform.position = pos;

            StartInteracting();
        }
    }

    void UpdateExiting()
    {
        int characterIndex = _missionMan.GetCharacterIndex();
        Transform spawnPoint = _spawnPoints[characterIndex];

        Vector3 dir = spawnPoint.position - _character.transform.position;
        dir.Normalize();

        if (_characterDef._rotateOnExit)
        {
            float singleStep = _rotSpeed * Time.deltaTime;
            Vector3 newDirection = Vector3.RotateTowards(_character.transform.forward, -dir, singleStep, 0.0f);
            _character.transform.rotation = Quaternion.LookRotation(newDirection);
        }

        _character.transform.position += (dir * _walkSpeed) * Time.deltaTime;
        if (_character.transform.position.z > spawnPoint.position.z)
        {
            // Reached the destination
            Vector3 pos = _character.transform.position;
            pos.z = spawnPoint.position.z;
            _character.transform.position = pos;

            _state = State.Invalid;

            // Destroy the character
            Destroy(_character.gameObject);

            _missionMan.CharacterFinished();
        }
    }

    void StartInteracting()
    {
        _currentTaskListIndex = 0;
        StartTaskList();
    }

    void StartTaskList()
    {
        _currentChats.Clear();
        _curTaskList = _characterDef._taskLists[_currentTaskListIndex];

        Debug.Log("StartTaskList " + _curTaskList.name);

        foreach (TaskListDef.Chat taskChat in _curTaskList._startChat)
            _currentChats.Add(taskChat);

        _taskMan.StartTaskList(_curTaskList);
        _state = State.Interacting;

        _currentChatIndex = -1;
        _pendingNextChat = true;
    }

    public void CompleteTaskList()
    {
        _currentChats.Clear();
        _currentChatIndex = -1;
        
        foreach (TaskListDef.Chat taskChat in _curTaskList._completeChat)
        {
            AddMessage(taskChat);
            _currentChats.Add(taskChat);
        }

        bool startChat = true;
        if (_currentChat != null)
            startChat = _currentChat._interruptible;

       // if (startChat)
            _pendingNextChat = true;
    }

    private void AddMessage(TaskListDef.Chat taskChat)
    {
        if (taskChat._audioClip != null)
            taskChat._chatTime = taskChat._audioClip._audioClip.length;
    }

    public void CompletedChat()
    {
        _taskMan.CompleteWaitForChat();

        if (_taskMan.HasCompletedTaskList())
        {
            // Move on to the next task list if there is one...
            _currentTaskListIndex++;
            if (_currentTaskListIndex < _characterDef._taskLists.Length)
            {
                StartTaskList();
            }
            else
            {
                // We've completed all the task lists - now exit
                _taskMan.CompletedTaskListAndChat();

                if (_characterDef._exitType == CharacterDef.ExitType.None)
                    _state = State.Invalid;
                else
                    _state = State.Exiting;
            }
        }
    }

    public void OnShowNextChat()
    {
        _currentChatIndex++;
        if (_currentChatIndex < _currentChats.Count)
        {
            // Act out the line
            _currentChat = _currentChats[_currentChatIndex];
            if ((_currentChat._actingClip != null)  || (_currentChat._audioClip != null))
                _character.PlayActingClip(_currentChat._actingClip, _currentChat._audioClip);

            if (_currentChat._audioClip != null)
                _chatTimer = _currentChat._audioClip._audioClip.length;
            else
                _chatTimer = _currentChat._chatTime;
        }
        else
        {
            _currentChat = null;

            // We've completed all the chats
            CompletedChat();
        }
    }
}

