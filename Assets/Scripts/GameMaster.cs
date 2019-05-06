using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMaster : MonoBehaviour {

    public static GameMaster gameMaster;


    private void Awake()
    {
        if (gameMaster)
        {
            Destroy(gameMaster.gameObject);
            gameMaster = this;
        }
        else { gameMaster = this; }
    }

    public void RestartLevel() {
        int buildIndex = SceneManager.GetActiveScene().buildIndex;

        StartCoroutine(LevelChangeTransition(buildIndex, 1f));
    }

    private IEnumerator LevelChangeTransition(int sceneIndex, float delay) {
        Animator cameraAnimator = GameObject.FindGameObjectWithTag("MainCamera").GetComponentInChildren<Animator>();
        cameraAnimator.SetTrigger("Exit");
        yield return new WaitForSeconds(delay);

        SceneManager.LoadScene(sceneIndex);
    }
}
