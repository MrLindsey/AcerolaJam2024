using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BlueScreenLogic : MonoBehaviour
{
    [SerializeField] private MusicMan _musicMan;

    public void OnPressQRCode()
    {
        // Restart the game
        SceneManager.LoadScene(0);
    }


    public void OnPressLink()
    {
        Application.OpenURL("https://www.youtube.com/channel/UC4_rMA-O2QXJyAcwMtWW8iw");
    }

    private void OnEnable()
    {
        // Show the cursor on the blue screen
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        _musicMan.StopAll();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
