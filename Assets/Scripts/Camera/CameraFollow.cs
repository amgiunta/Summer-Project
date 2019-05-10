using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {

    public Transform target;
    public PlayerNetwork player;
    public Vector3 offset;
    public Vector2 predictionAmount;

    public float cameraSpeed;
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
        if (player) { transform.localRotation = player.transform.localRotation; }

        if (!player.flipping)
        {
            position = player.GetFlipPivot(offset);
            if (prediction && player)
            {
                Vector2 velocity = player.rigidbody.velocity;
                velocity.Scale(predictionAmount);
                position += (Vector3) velocity;
            }

        }
        else {
            //transform.forward = player.transform.forward;
            position = new Vector3(player.transform.position.x, player.transform.position.y, offset.z);

            //transform.position = position;
        }

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
