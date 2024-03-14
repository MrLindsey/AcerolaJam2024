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
using UnityEngine.SceneManagement;

public class PauseMenuLogic : MonoBehaviour
{
    [SerializeField] private Transform _pauseScreen;

    private bool _isPaused;

    public void OnPressedResume()
    {
        PauseGame(false);
    }

    public void OnPressedRestart()
    {
        PauseGame(false);
        SceneManager.LoadScene(0);
    }

    public void OnPressedQuit()
    {
        Application.Quit();
    }

    // Start is called before the first frame update
    void Start()
    {
        _pauseScreen.gameObject.SetActive(false);
    }

    void PauseGame(bool onOff)
    {
        if (_isPaused != onOff)
        {
            if (onOff)
            {
                // Pause the game
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                _pauseScreen.gameObject.SetActive(true);
                AudioListener.pause = true;
                Time.timeScale = 0.0f;
            }
            else
            {
                // Unpause the game
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                _pauseScreen.gameObject.SetActive(false);
                AudioListener.pause = false;
                Time.timeScale = 1.0f;
            }

            _isPaused = onOff;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Application.platform == RuntimePlatform.WindowsPlayer ||
            Application.platform == RuntimePlatform.OSXPlayer ||
            Application.platform == RuntimePlatform.LinuxPlayer)
        {
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                PauseGame(!_isPaused);
            }
        }
    }
}
