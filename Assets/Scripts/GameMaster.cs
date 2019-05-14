using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The Game Master script will be used to manage the game scene and game data.
/// </summary>
public class GameMaster : MonoBehaviour {
    // The current working Game Master instance.
    public static GameMaster gameMaster;

    public Vector3 relativeGravityDirection = Vector3.down;
    
    private void Awake()
    {
        #region Singleton
        if (gameMaster)
        {
            Destroy(gameMaster.gameObject);
            gameMaster = this;
        }
        else { gameMaster = this; }
        #endregion
    }

    /// <summary>
    /// Re-loads the current open unity scene.
    /// </summary>
    public void RestartLevel() {
        // Create int build index that is the build index of the current active scene.
        int buildIndex = SceneManager.GetActiveScene().buildIndex;

        // Change the scene asynchronously.
        StartCoroutine(LevelChangeTransition(buildIndex, 1f));
    }

    public void ChangeRelativeGravity(Vector3 newGravity) {
        relativeGravityDirection = newGravity.normalized;
    }

    /// <summary>
    /// Asynchronously load and transition from one scene to another.
    /// </summary>
    /// <param name="sceneIndex">The build index of the scene to load.</param>
    /// <param name="delay">The delay in seconds to wait for before loading the scene.</param>
    /// <returns>An enumerator</returns>
    private IEnumerator LevelChangeTransition(int sceneIndex, float delay) {
        // Create Animator camera animator that is the Animator attached to the object in the scene with the tag "MainCamera".
        Animator cameraAnimator = GameObject.FindGameObjectWithTag("MainCamera").GetComponentInChildren<Animator>();
        // Set the 'Exit' trigger on the camera animator.
        cameraAnimator.SetTrigger("Exit");
        // Wait for the duration of delay
        yield return new WaitForSeconds(delay);

        // Use the Scene Manager to load the scene with the build index scene index.
        SceneManager.LoadScene(sceneIndex);
    }
}
