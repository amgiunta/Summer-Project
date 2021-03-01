using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {

    public static CameraFollow mainCamera;

    public enum CameraMode { Fixed, Zoom, Encapsulate, Default };
    public CameraMode cameraMode = CameraMode.Default;

    public List<Transform> targets;
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
    public float targetZoom;
    public float defaultZoom;
    public float margin;

    public float cameraSpeed;
    public float zoomSpeed;
    public float rotationSpeed;
    public bool prediction;

    //[HideInInspector]
    public CameraSpace currentSpace;

    private PlayerNetwork _player;
    [HideInInspector]
    public Bounds targetBounds;
    public Camera cam;

    private void Awake()
    {
        if (mainCamera) {
            Destroy(mainCamera.gameObject);
        }

        mainCamera = this;
    }

    // Use this for initialization
    void Start () {
        cam = GetComponentInChildren<Camera>();

        ResetZoom();
	}
	
	// Update is called once per frame
	void FixedUpdate () {
        MoveCamera();

        targetBounds = TargetBounds();
	}

    public void MoveCamera() {

        switch (cameraMode) {
            
            case CameraMode.Encapsulate:
                SetZoom(targetBounds.size.x > targetBounds.size.y ? targetBounds.size.x / 2 : targetBounds.size.y / 2);
                DefaultMovement();
                break;
            case CameraMode.Zoom:
            case CameraMode.Fixed:
            default:
                DefaultMovement();
                break;
        }
    }

    public void SetMode(CameraMode mode) {
        cameraMode = mode;
    }

    public void SetZoom(float zoom) {
        targetZoom = zoom + margin;
    }

    public void ResetZoom() {
        targetZoom = defaultZoom;
    }

    private void DefaultMovement() {
        if (player)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, TargetRotation(), Time.deltaTime * rotationSpeed);
        }

        transform.position = Vector3.Lerp(transform.position, TargetPosition(), cameraSpeed * Time.deltaTime);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * zoomSpeed);
    }

    private Quaternion TargetRotation() { 
        switch (cameraMode)
        {
            case CameraMode.Encapsulate:
            case CameraMode.Zoom:
            case CameraMode.Fixed:
            default:
                return Quaternion.LookRotation(Vector3.forward, player.transform.up);
        }
    }

    private Vector3 TargetPosition() {
        switch (cameraMode)
        {
            case CameraMode.Fixed:
            case CameraMode.Encapsulate:
                return new Vector3(targetBounds.center.x, targetBounds.center.y, offset.z);
            case CameraMode.Zoom:
            default:
                return new Vector3(player.transform.position.x, player.transform.position.y, offset.z);
        }
    }

    private Bounds TargetBounds() {
        if (targets.Count == 0) { return new Bounds(); }

        Bounds bounds = new Bounds(targets[0].position, Vector3.zero);
        foreach (Transform target in targets) {
            if (target == targets[0]) { continue; }
            bounds.Encapsulate(target.position);
        }

        return bounds;
    }

    private void OnDrawGizmos() {
    }

    
}
