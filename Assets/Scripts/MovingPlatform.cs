using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An abstracted Dynamic Platform that moves along a path with different modes. 
/// </summary>
public class MovingPlatform : DynamicPlatform {

    /// <summary>
    /// Movement modes for a Moving Platform
    /// </summary>
    public enum PathMode { Shuttle, Loop};
    /// <summary>
    /// The current mode of movement for this platform.
    /// </summary>
    [Tooltip("The current mode of movement for this platform.")]
    public PathMode pathMode;

    [Tooltip("The speed in m/s of this platform.")]
    public float speed;
    /// <summary>
    /// How long this platform will wait (in seconds) before moving to the next destination."
    /// </summary>
    [Tooltip("How long this platform will wait (in seconds) before moving to the next destination.")]
    public float delay;
    /// <summary>
    /// A list of transforms to use as an ordered path for this platform to move along.
    /// </summary>
    [Tooltip("A list of transforms to use as an ordered path for this platform to move along.")]
    public List<Transform> pathPoints = new List<Transform>();

    /// <summary>
    /// The point in the path that the platform is currently at.
    /// </summary>
    Transform currentPathPoint;

    /// <summary>
    /// Flag to move the platform when possible.
    /// </summary>
    [HideInInspector]
    public bool movePlatform;
    /// <summary>
    /// Flag when the platform is currently moving to a new location.
    /// </summary>
    [HideInInspector]
    public bool isMoving;
    /// <summary>
    /// Should the platform automatically move when it hits the target location?
    /// </summary>
    [Tooltip("Should the platform automatically move when it hits the target location?")]
    public bool continuous;

    /// <summary>
    /// Direction to cycle positions in the pathPoint list. (1 for +, -1 for -).
    /// </summary>
    private int pathDirection = 1;

    public virtual void Start() {
        // Set the current point to the first path point
        currentPathPoint = pathPoints[0];
        // Set the current object's location to the position of the current path point.
        transform.position = currentPathPoint.position;
    }

    public override void FixedUpdate() {
        // If continuous, set the move flag to true.
        if (continuous) { movePlatform = true; }

        // Should the platform move, and it's not already moving, move the platform.
        if (movePlatform && !isMoving) { StartCoroutine(MoveToPoint(GetNextPoint())); }
        // Call the base Fixed update.
        base.FixedUpdate();
    }

    /// <summary>
    /// Move the platform.
    /// </summary>
    public virtual void ActivatePlatform() {
        // Set move platform flag to true.
        movePlatform = true;
    }

    public virtual void MoveToPoint(int pointIndex) {
        if (!isMoving)
            StartCoroutine(MoveToPoint(pathPoints[pointIndex]));
    }

    /// <summary>
    /// Get the next point in the path.
    /// </summary>
    /// <returns>nextpointinpath</returns>
    protected virtual Transform GetNextPoint() {
        // If the point path is empty, return the 0th entry. (or empty list) 
        if (!pathPoints.Contains(currentPathPoint)) { return pathPoints[0]; }
        // Create int pointIndex that gets the value of: the index of the current path point in the path.
        int pointIndex = pathPoints.IndexOf(currentPathPoint);

        // If the point index is the last point in the path
        if (pointIndex == pathPoints.Count - 1)
        {
            // and the pathmode is "Shuttling"
            if (pathMode == PathMode.Shuttle)
            {
                // Set the path direction to negative;
                pathDirection = -1;
            }
            else
            {
                // return the first point in the path.
                return pathPoints[0];
            }
        }
        // The point index is 0 and the pathmode is "Shuttling"
        else if (pointIndex == 0 && pathMode == PathMode.Shuttle)
        {
            // Set the path direction to positive.
            pathDirection = 1;
        }

        // Return the point in the path at index: path index plus path direction.
        return pathPoints[pointIndex + pathDirection];
    }

    /// <summary>
    /// Asynchronous function to move the platform to a location over time.
    /// </summary>
    /// <returns>An enumerator</returns>
    [System.Obsolete("Use 'IEnumerator MoveToPoint(int)' instead.")]
    protected virtual IEnumerator MoveEnumerator() {
        // Set move platform flag to false.
        movePlatform = false;
        // Set the movement flag to true.
        isMoving = true;
        // Wait for the delay time.
        yield return new WaitForSeconds(delay);
        // Create transform nextPoint that is the next point in path.
        Transform nextPoint = GetNextPoint();
        // Create 3D point that is the poition of this object
        Vector3 startPos = transform.position;
        // Create float that is the distance between this object and the position of the next point
        float distance = Vector2.Distance(transform.position, nextPoint.position);
        // Create float time that is the duration of the algorythm: calculated from t = d/v where d = distance, v = speed.
        float time = distance/speed;

        // Loop the following for every step of stride of one frame in seconds between t = 0 and the value for time.
        for (float t = 0; t <= time; t+= Time.deltaTime ) {
            // Create float percent that is the percent of t over time.
            float percent = t / time;

            // Set the position of this object to: the linear interpolation of percent on line between startPos and the position of next point.
            transform.position = Vector3.Lerp(startPos, nextPoint.position, percent);
            // Wait for the duration of 1 frame.
            yield return new WaitForEndOfFrame();
        }

        // Set the position of this object to the position of next point
        transform.position = nextPoint.position;
        // Set the current path point to the next point.
        currentPathPoint = nextPoint;
        // Set the movement flag to false.
        isMoving = false;
    }

    IEnumerator MoveToPoint(Transform point) {
        // Set move platform flag to false.
        movePlatform = false;
        // Set the movement flag to true.
        isMoving = true;
        // Wait for the delay time.
        yield return new WaitForSeconds(delay);
        // Create 3D point that is the poition of this object
        Vector3 startPos = transform.position;
        // Create float that is the distance between this object and the position of the next point
        float distance = Vector2.Distance(transform.position, point.position);
        // Create float time that is the duration of the algorythm: calculated from t = d/v where d = distance, v = speed.
        float time = distance / speed;

        // Loop the following for every step of stride of one frame in seconds between t = 0 and the value for time.
        for (float t = 0; t <= time; t += Time.deltaTime)
        {
            // Create float percent that is the percent of t over time.
            float percent = t / time;

            // Set the position of this object to: the linear interpolation of percent on line between startPos and the position of next point.
            transform.position = Vector3.Lerp(startPos, point.position, percent);
            // Wait for the duration of 1 frame.
            yield return new WaitForEndOfFrame();
        }

        // Set the position of this object to the position of next point
        transform.position = point.position;
        // Set the current path point to the next point.
        currentPathPoint = point;
        // Set the movement flag to false.
        isMoving = false;
    }

    private void OnDrawGizmos() {
        // If the path is empty, stop algorythm.
        if (pathPoints.Count == 0) { return; }

        // Create transform start point that is the first point in path
        Transform startPoint = pathPoints[0];
        // Create transform "prev" that is the start point.
        Transform prev = startPoint;

        // For every point in path.
        foreach (Transform point in pathPoints) {
            // If the point is the start point, skip to next iteration.
            if (point == startPoint) { continue; }

            // If the point is the last point in path, and the pathmode is "Looping,"
            if (point == pathPoints[pathPoints.Count - 1] && pathMode == PathMode.Loop)
            {
                // [DEBUG] Draw a blue line between the position of the point and the position of the start point.
                Debug.DrawLine(point.position, startPoint.position, Color.blue);
            }

            // [DEBUG] Draw a blue line between the position of prev and the position of the point. 
            Debug.DrawLine(prev.position, point.position, Color.blue);
        }
    }
}
