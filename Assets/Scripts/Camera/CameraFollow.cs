using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {

    public Transform target;
    public PlayerNetwork player {
        get {
            if (!_player) {
                _player = FindObjectOfType<PlayerNetwork>();
            }

            return _player;
        }
    }

    public Vector3 offset;
    public Vector2 predictionAmount;

    public float cameraSpeed;
    public float rotationSpeed;
    public bool prediction;

    private PlayerNetwork _player;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void FixedUpdate () {
        MoveCamera();
	}

    public void MoveCamera(out Vector3 position) {

        if (player) {
            transform.rotation = Quaternion.Slerp(transform.rotation, player.transform.rotation, Time.deltaTime * rotationSpeed);
        }

        position = new Vector3(player.transform.position.x, player.transform.position.y, offset.z);

        
        //if (!player.isFliping)
        //{
        //    position = player.transform.position + offset;
        //    if (prediction && player)
        //    {
        //        Vector2 velocity = player.rigidbody.velocity;
        //        velocity.Scale(predictionAmount);
        //        position += (Vector3) velocity;
        //    }

        //}
        //else {
        //    transform.forward = player.transform.forward;
        //    position = new Vector3(player.transform.position.x, player.transform.position.y, offset.z);

        //    transform.position = position;
        //}
        

        transform.position = Vector3.Lerp(transform.position, position, cameraSpeed * Time.deltaTime);

    }

    public void MoveCamera() {
        Vector3 position;
        MoveCamera(out position);
    }

    private void OnDrawGizmos() {
    }

    
}
