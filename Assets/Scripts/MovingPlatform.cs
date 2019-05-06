using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : DynamicPlatform {

    public enum PathMode { Shuttle, Loop};
    public PathMode pathMode;

    public float speed;
    public float delay;
    public List<Transform> pathPoints = new List<Transform>();
    [HideInInspector]
    Transform currentPathPoint;

    //[HideInInspector]
    public bool movePlatform;
    //[HideInInspector]
    public bool isMoving;
    public bool continuous;

    private int pathDirection = 1;
    private float _deltaTime;

    public virtual void Start() {
        currentPathPoint = pathPoints[0];
        transform.position = currentPathPoint.position;
    }

    public override void FixedUpdate() {
        _deltaTime = Time.deltaTime;
        if (continuous) { movePlatform = true; }

        if (movePlatform && !isMoving) { StartCoroutine(MoveEnumerator()); }
        base.FixedUpdate();
    }

    public virtual void ActivatePlatform() {
        movePlatform = true;
    }

    protected virtual Transform GetNextPoint() {
        if (!pathPoints.Contains(currentPathPoint)) { return pathPoints[0]; }
        int pointIndex = pathPoints.IndexOf(currentPathPoint);

        if (pointIndex == pathPoints.Count - 1)
        {
            if (pathMode == PathMode.Shuttle)
            {
                pathDirection = -1;
                return pathPoints[pointIndex + pathDirection];
            }
            else
            {
                return pathPoints[0];
            }
        }
        else if (pointIndex == 0 && pathMode == PathMode.Shuttle)
        {
            pathDirection = 1;
            return pathPoints[pointIndex + pathDirection];
        }
        else { return pathPoints[pointIndex + pathDirection]; }
    }

    protected virtual IEnumerator MoveEnumerator() {
        movePlatform = false;
        isMoving = true;
        yield return new WaitForSeconds(delay);
        Transform nextPoint = GetNextPoint();
        Vector3 startPos = transform.position;
        float distance = Vector2.Distance(transform.position, nextPoint.position);
        float time = distance/speed;

        for (float t = 0; t <= time; t+= Time.deltaTime ) {
            time = distance / speed;
            float percent = t / time;

            transform.position = Vector3.Lerp(startPos, nextPoint.position, percent);
            yield return new WaitForEndOfFrame();
        }

        transform.position = nextPoint.position;
        currentPathPoint = nextPoint;
        isMoving = false;
    }

    private void OnDrawGizmos() {
        if (pathPoints.Count == 0) { return; }
        Transform startPoint = pathPoints[0];
        Transform prev = startPoint;

        foreach (Transform point in pathPoints) {
            if (point == startPoint) { continue; }

            if (point == pathPoints[pathPoints.Count - 1] && pathMode == PathMode.Loop)
            {
                Debug.DrawLine(point.position, startPoint.position, Color.blue);
            }

            Debug.DrawLine(prev.position, point.position, Color.blue);
        }
    }
}
