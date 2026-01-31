using PrimeTween;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartGame : MonoBehaviour
{

    public Button playGame, restartGame, exitGame;

    void Awake()
    {
        PrimeTweenConfig.warnZeroDuration = false;
        PrimeTweenConfig.warnEndValueEqualsCurrent = false;
        PrimeTweenConfig.defaultEase = Ease.Linear;

        if (playGame != null)
        {
            playGame.onClick.AddListener(OnStartGame);
        }

        if (restartGame != null)
        {
            restartGame.onClick.AddListener(OnRestartGame);
        }

        if (exitGame != null)
        {
            exitGame.onClick.AddListener(OnExitGame);
        }

        AudioMgr.Instance.Init();

        Utils.initGame = true;
    }


    public void OnStartGame()
    {
        Utils.initGame = true;
        GameResetService.ResetSession();
        SceneManager.LoadScene(GameSceneNames.MainScene);
    }

    public void OnRestartGame()
    {
        Utils.initGame = true;
        GameResetService.ResetSession();
        GameResetService.ResetProgressForNewGame(keepLanguage: true);
        SceneManager.LoadScene(GameSceneNames.MainScene);
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
