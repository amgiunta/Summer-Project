using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {

    public Transform target;
    public PlayerNetwork player;
    public Vector3 offset;
    public float predictionAmount;

    public float cameraSpeed;
    public float rotationSpeed;
    public bool prediction;

	// Use this for initialization
	void Start () {
        FindPlayer();
	}
	
	// Update is called once per frame
	void Update () {
        if (!player) { FindPlayer(); }

        MoveCamera();
	}

    void MoveCamera(out Vector3 position) {
        position = player.GetFlipPivot(offset);
        if (prediction && player) {
            position += new Vector3(player.rigidbody.velocity.normalized.x * predictionAmount, 0f);
        }

        if (player) { transform.localRotation = player.transform.localRotation; }

        transform.position = Vector3.Lerp(transform.position, position, cameraSpeed * Time.deltaTime);
    }

    void MoveCamera() {
        Vector3 position;
        MoveCamera(out position);
    }

    public void FindPlayer() {
        player = FindObjectOfType<PlayerNetwork>();

        if (!player) { Debug.LogError("Could not locate player! Check to see if the player is in the scene, tagged as 'Player', and has the PlayerNetwork (or some derivative behavior) attached.", this); }
        else { target = player.transform; }
    }

    private void OnDrawGizmos() {
        if (!player) { FindPlayer(); }
        MoveCamera();
    }
}
