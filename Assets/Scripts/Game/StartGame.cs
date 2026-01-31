using PrimeTween;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartGame : MonoBehaviour
{

    public Button playGame, exitGame;

    void Awake()
    {
        PrimeTweenConfig.warnZeroDuration = false;
        PrimeTweenConfig.warnEndValueEqualsCurrent = false;
        PrimeTweenConfig.defaultEase = Ease.Linear;

        playGame.onClick.AddListener(OnStartGame);
        exitGame.onClick.AddListener(OnExitGame);

        AudioMgr.Instance.Init();

        Utils.initGame = true;
    }

    // Start is called before the first frame update
    void Start()
    {
        SceneManager.LoadScene("MainScene");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnStartGame()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void OnExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
