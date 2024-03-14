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

public class MainMenuLogic : MonoBehaviour
{
    [SerializeField] private PlayerController _player;
    [SerializeField] private Transform[] _doors;

    [SerializeField] private SocketLogic[] _startSockets;
    [SerializeField] private TaskListDef _extensionPlugTaskList;

    [SerializeField] private Transform _plugExtension;
    [SerializeField] private Transform _teleportSam;
    [SerializeField] private Transform _teleportSamCollision;
    [SerializeField] private Transform _cutSceneSamCam;
    [SerializeField] private FuseMan _fuseMan;
    [SerializeField] private Transform _blueScreen;
    [SerializeField] private Transform _blackBars;
    [SerializeField] private float _alarmSpeed;
    [SerializeField] private Color _alarmColour = Color.red;
    [SerializeField] private string _lightmapTintName = "_LightmapTint";
    [SerializeField] private Color _powerOffColour = Color.gray;
    [SerializeField] private AudioSource _alarmSound;
    [SerializeField] private SfxPlayer _sfxPlayer;
    [SerializeField] private Transform _startButton;
    [SerializeField] private Color _lowerTintColour = Color.white;
    [SerializeField] private float _lowerTintDepth = -10.0f;
    [SerializeField] private Color _upperTintColour = Color.white;
    [SerializeField] private float _upperTintDepth = 12.0f;
    [SerializeField] private TextMeshProUGUI _screenText;
    [SerializeField] private string _screenTextString = "UNEXPECTED PANIC!";
    [SerializeField] private string _screenSecurityString = "SECURITY ALERT!";
    [SerializeField] private float _startingGameTime = 5.0f;

    [SerializeField] private TextMeshProUGUI _startButtonText;

    private TaskMan _taskMan;
    private MissionMan _missionMan;
    private CharacterMan _characterMan;
    private int _doorIndex;
    private CursorGrabInteractor _grabInteractor;
    private Animation _blackBarsAnim;
    private bool _alarmActive;
    private float _alarmTimer;
    private Color _currentTintColour;

    public void OnTaskStarted()
    {
        // Do specific main menu things on specific tasks when started
        TaskDef task = _taskMan.GetLastStartedTask();

        string name = task._name;
        if (name == "PickupStartPlug")
        {
            _doorIndex = 0;
            Invoke("OpenDoor", 3.0f);
        }
        else if (name == "Press Start")
        {
            _startButton.GetComponent<GrabbableObjectController>().enabled = true;
        }
        else if (name == "FindExtension")
        {
            GrabbableSwitcher switcher = _plugExtension.GetComponent<GrabbableSwitcher>();
            switcher.SetGrabbable(true);
        }
        else if (name == "GotoPowerRoom")
        {
            SetPowerDown(true);
            _doorIndex = 1;
            Invoke("OpenDoor", 1.0f);
        }
        else if (name == "SamIntro")
        {
            _doorIndex = 1;
            Invoke("CloseDoor", 0.0f);
            Invoke("TeleportSam", 14.0f);

            AllowGrabbing(false);
            _player.SetCutsceneMode(true, _cutSceneSamCam);
        }
        else if (name == "FuseSneezingIntro")
        {
            _fuseMan.Activate();
            _fuseMan.PlaySneezeAudio(false);
        }
        else if (name == "FuseSneezing")
        {
            _fuseMan.PlaySneezeAudio(true);
        }
        else if (name == "SecurityActivated")
        {
            SetAlarm(true);
            _doorIndex = 2;
            Invoke("OpenDoor", 0.0f);
        }
        else if (name == "KillBot")
        {
            _doorIndex = 2;
            Invoke("CloseDoor", 0.0f);
        }
    }

    public void OnTaskComplete()
    {
        // Do specific main menu things on specific tasks when started
        TaskDef task = _taskMan.GetLastCompletedTask();

        string name = task._name;
        if (name == "GotoPowerRoom" || name == "AfterFuse" || name == "SecurityActivated" || name == "BotDeath")
        {
            _missionMan.CharacterFinished();
        }

        else if (name == "Press Start")
        {
            _sfxPlayer.PlayClip(SfxPlayer.SfxTypes.BreakButton);
            SetStartButtonStatus(false);
        }
        else if (name == "UseExtensionCable")
        {
            SetStartButtonStatus(true);
        }
        else if (name == "StartButtonFinal")
        {
            _missionMan.CharacterFinished();
            StartBlueScreenSequence();
        }
        else if (name == "SamIntro" || name == "FuseSneezingIntro")
        {
            AllowGrabbing(true);
            _player.SetCutsceneMode(false, _cutSceneSamCam);
        }
        else if (name == "FuseSneezing")
        {
            _doorIndex = 1;
            Invoke("OpenDoor", 1.0f);
            SetPowerDown(false);
        }
        else if (name == "KillBot")
        {
            SetAlarm(false);
            _doorIndex = 2;
            Invoke("OpenDoor", 1.0f);
        }
    }

    void StartBlueScreenSequence()
    {
        _screenText.text = "Starting Game...";
        Invoke("ShowBlueScreen", _startingGameTime);
    }

    void ShowBlueScreen()
    {
        _blueScreen.gameObject.SetActive(true);
        // Play the VO for the end game
    }


    void OpenDoor()
    {
        if (_doorIndex == 0)
        {
            Animation anim = _doors[0].GetComponent<Animation>();
            anim.Play();
        }
        else
        {
            _doors[_doorIndex].gameObject.SetActive(false);
        }
    }

    void CloseDoor()
    {
        _doors[_doorIndex].gameObject.SetActive(true);
    }

    void AllowGrabbing(bool onOff)
    {
        _grabInteractor.AllowGrabbing(onOff);
        _blackBars.gameObject.SetActive(!onOff);

        if (onOff == false)
        {
            _blackBarsAnim.Rewind();
            _blackBarsAnim.Play();
        }
    }

    void TeleportSam()
    {
        // Don't do this now... the cutscene works better
        //Character character = _characterMan.GetCurrentCharacter();
        //character.transform.position = _teleportSam.position;
        //_teleportSamCollision.gameObject.SetActive(false);

        _fuseMan.TeleportFuses();
    }

    private void Awake()
    {
        _doorIndex = 0;
        _taskMan = GetComponent<TaskMan>();
        _missionMan = GetComponent<MissionMan>();
        _characterMan = GetComponent<CharacterMan>();
        _grabInteractor = _player.GetComponent<CursorGrabInteractor>();
        _blackBarsAnim = _blackBars.GetComponent<Animation>();

        _blueScreen.gameObject.SetActive(false);
        _blackBars.gameObject.SetActive(false);

        // Set the lighting to white to start with
        SetLightTintColour(Color.white);

        _startButton.GetComponent<GrabbableObjectController>().enabled = false;

    }

    private void SetLightTintColour(Color color)
    {
        _currentTintColour = color;
        Shader.SetGlobalColor(_lightmapTintName, _currentTintColour);
    }

    private void SetAlarm(bool onOff)
    {
        _alarmActive = onOff;
        if (onOff)
        {
            _alarmSound.Play();
            _screenText.text = _screenSecurityString;
        }
        else
        {
            SetLightTintColour(Color.white);
            _alarmSound.Stop();
            _screenText.text = _screenTextString;
        }

        SetStartButtonStatus(!onOff);
    }

    private void SetStartButtonStatus(bool onOff)
    {
        if (onOff)
        {
            Color color = Color.white;
            color.a = 0.9f;
            _startButtonText.color = color;
        }
        else
        {
            Color color = Color.black;
            color.a = 0.2f;
            _startButtonText.color = color;
        }
    }

    private void SetPowerDown(bool onOff)
    {
        if (onOff)
        {
            SetLightTintColour(_powerOffColour);
            _sfxPlayer.PlayClip(SfxPlayer.SfxTypes.PowerDown);
            _screenText.text = "";
        }
        else
        {
            SetLightTintColour(Color.white);
            _screenText.text = _screenTextString;
        }

        SetStartButtonStatus(!onOff);
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

        // Pulse the alarm if it's on
        if (_alarmActive)
        {
            // Pulse the alarm colour
            _alarmTimer += Time.deltaTime;
            float delta = (1.0f + Mathf.Sin(_alarmTimer * _alarmSpeed)) * 0.5f;
            Color alarmColour = Color.Lerp(RenderSettings.ambientLight, _alarmColour, delta);
            SetLightTintColour(alarmColour);
        }


        // The lower the player goes in the level, the closer towards the loewr tint colour it goes
        float playerY = _player.transform.position.y;
        if (playerY < 0.0f)
        {
            float tintBlend = (_lowerTintDepth - playerY) / _lowerTintDepth;
            Color blendedColor = Color.Lerp(_lowerTintColour, _currentTintColour, tintBlend);
            Shader.SetGlobalColor(_lightmapTintName, blendedColor);
        }

        if (playerY > 0.0f)
        {
            float tintBlend = (_upperTintDepth - playerY) / _upperTintDepth;
            Color blendedColor = Color.Lerp(_upperTintColour, _currentTintColour, tintBlend);
            Shader.SetGlobalColor(_lightmapTintName, blendedColor);
        }
    }
}
