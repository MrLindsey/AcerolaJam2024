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

public class MainMenuLogic : MonoBehaviour
{
    [SerializeField] private PlayerController _player;
    [SerializeField] private Transform[] _doors;

    [SerializeField] private SocketLogic[] _startSockets;
    [SerializeField] private TaskListDef _extensionPlugTaskList;

    [SerializeField] private Transform _plugExtension;
    [SerializeField] private Transform _teleportSam;
    [SerializeField] private Transform _teleportSamCollision;
    [SerializeField] private FuseMan _fuseMan;
    [SerializeField] private Transform _blueScreen;

    private TaskMan _taskMan;
    private MissionMan _missionMan;
    private CharacterMan _characterMan;
    private int _doorIndex;
    private CursorGrabInteractor _grabInteractor;

    public void OnTaskStarted()
    {
        // Do specific main menu things on specific tasks when started
        TaskDef task = _taskMan.GetLastStartedTask();

        if (task._name == "PickupStartPlug")
        {
            _doorIndex = 0;
            Invoke("OpenDoor", 3.0f);
        }
        else if (task._name == "FindExtension")
        {
            _plugExtension.gameObject.SetActive(true);
        }
        else if (task._name == "GotoPowerRoom")
        {
            _doorIndex = 1;
            Invoke("OpenDoor", 1.0f);
        }
        else if (task._name == "SamIntro")
        {
            _doorIndex = 1;
            Invoke("CloseDoor", 0.0f);

            Invoke("TeleportSam", 14.0f);
            _grabInteractor.AllowGrabbing(false);
        }
        else if (task._name == "FuseSneezingIntro")
        {
            _grabInteractor.AllowGrabbing(false);
        }
        else if (task._name == "FuseSneezing")
        {
            _fuseMan.Activate();
        }
        else if (task._name == "SecurityActivated")
        {
            _doorIndex = 2;
            Invoke("OpenDoor", 0.0f);
        }
        else if (task._name == "KillBot")
        {
            _doorIndex = 2;
            Invoke("CloseDoor", 0.0f);
        }
    }

    public void OnTaskComplete()
    {
        // Do specific main menu things on specific tasks when started
        TaskDef task = _taskMan.GetLastCompletedTask();

        if (task._name == "GotoPowerRoom" || task._name == "AfterFuse" || task._name == "SecurityActivated" || task._name == "BotDeath")
        {
            _missionMan.CharacterFinished();
        }
        else if (task._name == "StartButtonFinal")
        {
            _missionMan.CharacterFinished();
            _blueScreen.gameObject.SetActive(true);
        }
        else if (task._name == "SamIntro" || task._name == "FuseSneezingIntro")
        {
            _grabInteractor.AllowGrabbing(true);
        }
        else if (task._name == "FuseSneezing")
        {
            _doorIndex = 1;
            Invoke("OpenDoor", 1.0f);
        }
        else if (task._name == "KillBot")
        {
            _doorIndex = 2;
            Invoke("OpenDoor", 1.0f);
        }
    }

    void OpenDoor()
    {
        _doors[_doorIndex].gameObject.SetActive(false);
    }

    void CloseDoor()
    {
        _doors[_doorIndex].gameObject.SetActive(true);
    }

    void TeleportSam()
    {
        Character character = _characterMan.GetCurrentCharacter();
        character.transform.position = _teleportSam.position;
        _teleportSamCollision.gameObject.SetActive(false);

    }

    private void Awake()
    {
        _doorIndex = 0;
        _taskMan = GetComponent<TaskMan>();
        _missionMan = GetComponent<MissionMan>();
        _characterMan = GetComponent<CharacterMan>();
        _grabInteractor = _player.GetComponent<CursorGrabInteractor>();

        _plugExtension.gameObject.SetActive(false);
        _blueScreen.gameObject.SetActive(false);
    }

    private void Update()
    {
        TaskListDef currentTasks = _taskMan.GetCurrentTaskList();

        if (currentTasks != null)
        {
            // Check if the plugs have been connected for the extension plug task
            if (currentTasks == _extensionPlugTaskList)
            {
                bool allPlugsConnected = true;
                foreach (SocketLogic socket in _startSockets)
                {
                    if (socket.IsConnected() == false)
                    {
                        allPlugsConnected = false;
                        break;
                    }
                }

                // We've connected all the start sockets
                if (allPlugsConnected)
                {
                    _taskMan.CompleteFromScript("Sockets");
                }
            }
        }
    }
}
