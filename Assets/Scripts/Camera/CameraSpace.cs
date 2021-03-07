using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class CameraSpace : MonoBehaviour
{

    public CameraFollow.CameraMode cameraMode;

    public UnityEvent onSpaceEnter;
    public UnityEvent onSpaceExit;

    public List<Transform> targets;
    public float zoom;

    new private Collider2D collider;

    private void Start()
    {
        switch (cameraMode)
        {
            case CameraFollow.CameraMode.Fixed:
                onSpaceEnter.AddListener(() => {
                    CameraFollow.mainCamera.SetMode(cameraMode);
                    CameraFollow.mainCamera.targets.Add(transform);
                    CameraFollow.mainCamera.SetZoom(zoom);
                });
                break;
            case CameraFollow.CameraMode.Zoom:
                onSpaceEnter.AddListener(() => { 
                    CameraFollow.mainCamera.SetMode(cameraMode);
                    CameraFollow.mainCamera.SetZoom(zoom);
                });
                break;
            case CameraFollow.CameraMode.Encapsulate:
                onSpaceEnter.AddListener(() => { 
                    CameraFollow.mainCamera.SetMode(cameraMode);
                    CameraFollow.mainCamera.targets.AddRange(targets);
                });
                break;
            default:
                onSpaceEnter.AddListener(() => {
                    CameraFollow.mainCamera.SetMode(cameraMode);
                    CameraFollow.mainCamera.ResetZoom();
                    CameraFollow.mainCamera.targets.Clear();
                });
                break;
        }

        onSpaceExit.AddListener(() => {
            if (CameraFollow.mainCamera.currentSpace == this)
            {
                CameraFollow.mainCamera.SetMode(CameraFollow.CameraMode.Default);
                CameraFollow.mainCamera.ResetZoom();

                CameraFollow.mainCamera.currentSpace = null;
            }

            if (cameraMode == CameraFollow.CameraMode.Encapsulate)
            {
                foreach (Transform target in targets)
                {
                    CameraFollow.mainCamera.targets.Remove(target);
                }
            }
            else if (cameraMode == CameraFollow.CameraMode.Fixed) {
                CameraFollow.mainCamera.targets.Remove(transform);
            }
        });

        collider = GetComponent<Collider2D>();
        collider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) {
            CameraFollow.mainCamera.currentSpace = this;
            onSpaceEnter.Invoke();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) {
            onSpaceExit.Invoke();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;

        switch (cameraMode) {
            case CameraFollow.CameraMode.Zoom:
            case CameraFollow.CameraMode.Fixed:
                Vector2 botLeft = new Vector2(transform.position.x - (16 * zoom / 9), transform.position.y - zoom);
                Vector2 topLeft = new Vector2(transform.position.x - (16 * zoom / 9), transform.position.y + zoom);
                Vector2 topRight = new Vector2(transform.position.x + (16 * zoom / 9), transform.position.y + zoom);
                Vector2 botRight = new Vector2(transform.position.x + (16 * zoom / 9), transform.position.y - zoom);
                Debug.DrawLine(botLeft, topLeft, Gizmos.color);
                Debug.DrawLine(topLeft, topRight, Gizmos.color);
                Debug.DrawLine(topRight, botRight, Gizmos.color);
                Debug.DrawLine(botRight, botLeft, Gizmos.color);
                break;
            default:
                break;
        }
    }
}
