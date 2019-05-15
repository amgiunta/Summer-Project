using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RelativeGravity : MonoBehaviour
{
    public LayerMask groundLayers;
    public float yOffset;
    [Space]

    public bool alignToSurface;
    public bool flipWithPlayer;
    public bool invertGravity;
    public bool customGravity;
    [Range(0, 180)]
    public float maxAngle;
    [Range(0.1f, 10)]
    public float primarySurfaceDistance;
    [Range(0.1f, 10)]
    public float maxSurfaceDistance = 1;
    public Vector3 customGravityDirection;
    [Range(0.1f, 10)]
    public float gravityScale = 1;

    SurfacePoint surface;

    Rigidbody2D rigidbody;

    Vector3 _relativeGravity;
    Vector3 _center;

    // Start is called before the first frame update
    void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();

        rigidbody.gravityScale = 0;
        customGravityDirection.Normalize();
    }

    // Update is called once per frame
    void Update()
    {
        _center = transform.up * yOffset;
    }

    protected virtual void ApplyGravity() {
        if (customGravity)
        {
            _relativeGravity = customGravityDirection.normalized * rigidbody.mass * gravityScale * -Physics2D.gravity.y;
        }
        else {
            if (alignToSurface) {
                surface = GetSurfacePoint(maxSurfaceDistance);
                if (surface) {
                    // rotate object to have the same normal as the surface point
                    transform.rotation = Quaternion.LookRotation(Vector3.forward, surface.normal);
                    // Set relative gravity to oposite the normal
                    _relativeGravity = -surface.normal * rigidbody.mass * gravityScale * -Physics2D.gravity.y;
                }
            }
        }

        if (invertGravity) { _relativeGravity *= -1; }
        if (Application.isPlaying) {

            rigidbody.AddForce(_relativeGravity);
        }
    }


    public SurfacePoint GetSurfacePoint(float maxDistance) {
        SurfacePoint surface = null;
        SurfacePoint leftFoot = null;
        SurfacePoint rightFoot = null;
        SurfacePoint sateliteSurface = SurfacePointInArea(maxDistance);
        surface = SurfacePointBelow(out leftFoot, out rightFoot);


        if (leftFoot || rightFoot) { return surface; }

        if (sateliteSurface && Vector2.Angle(transform.up, sateliteSurface.normal) <= maxAngle) { return sateliteSurface; }

        return surface;
    }

    private SurfacePoint SurfacePointBelow(out SurfacePoint leftAngle, out SurfacePoint rightAngle) {
        leftAngle = Physics2D.Raycast(transform.position + _center, Quaternion.Euler(0, 0, -maxAngle / 2) * -transform.up, primarySurfaceDistance, groundLayers);
        rightAngle = Physics2D.Raycast(transform.position + _center, Quaternion.Euler(0, 0, maxAngle / 2) * -transform.up, primarySurfaceDistance, groundLayers);

        //RaycastHit2D hit;

        if (leftAngle) { Debug.DrawLine(transform.position + _center, leftAngle.position, Color.red); }
        else { Debug.DrawRay(transform.position + _center, Quaternion.Euler(0, 0, -maxAngle / 2) * -transform.up * primarySurfaceDistance, Color.yellow); }

        if (rightAngle) { Debug.DrawLine(transform.position + _center, rightAngle.position, Color.red); }
        else { Debug.DrawRay(transform.position + _center, Quaternion.Euler(0, 0, maxAngle / 2) * -transform.up * primarySurfaceDistance, Color.yellow); }

        if (leftAngle && rightAngle)
        {
            /*
            Vector2 point;

            
            if (leftAngle.point.collider == rightAngle.point.collider)
            {
                point = leftAngle.point.collider.ClosestPoint(transform.position);
            }
            else {
                Vector2 lPoint = leftAngle.point.collider.ClosestPoint(transform.position);
                Vector2 rPoint = rightAngle.point.collider.ClosestPoint(transform.position);

                if (Vector2.Distance(lPoint, transform.position) < Vector2.Distance(rPoint, transform.position)) { point = lPoint; }
                else { point = rPoint; }
            }
            */


            if (Vector2.Angle(transform.up, leftAngle.normal) < Vector2.Angle(transform.up, rightAngle.normal)) { return leftAngle; }
            else return rightAngle;

            //RaycastHit2D newHit = Physics2D.Raycast(transform.position + _center, direction, primarySurfaceDistance, groundLayers);

            //return newHit;
        }
        else { return null; }
    }

    private SurfacePoint SurfacePointInArea(float radius) {
        // Get list of ground colliders in radius.
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position + _center, radius, groundLayers);
        // if no colliders are found, return null
        if (colliders.Length == 0) { return null; }

        // Get list of the closest point to this object from each collider
        List<SurfacePoint> points = new List<SurfacePoint>();
        RaycastHit2D hitPoint;
        Vector3 closestPoint;
        for (int i = 0; i < colliders.Length; i++) {
            closestPoint = colliders[i].ClosestPoint(transform.position);
            Vector3 heading = closestPoint - transform.position;
            Vector3 direction = heading / heading.magnitude;
            hitPoint = Physics2D.Raycast(transform.position, direction, groundLayers);
            if (hitPoint) {
                if (!points.Contains(hitPoint)) {
                    points.Add(hitPoint);
                    Debug.DrawLine(transform.position + _center, hitPoint.point, Color.red);
                    
                }
            }
        }

        SurfacePoint closestSurfacePoint = points[0];
        foreach (SurfacePoint point in points) {
            if (Vector3.Distance(point.position, transform.position) < Vector3.Distance(closestSurfacePoint.position, transform.position)) {
                closestSurfacePoint = point;
            }
        }

        return closestSurfacePoint;
    }

    private void OnDrawGizmosSelected()
    {
        if (!rigidbody)
            Start();

        ApplyGravity();
        //Debug.DrawRay(transform.position, _relativeGravity, Color.red);

        Gizmos.color = Color.yellow;
        //Vector3 leftConeSideDirection = Quaternion.Euler(0,0,-maxAngle/2) * -transform.up;
        //Vector3 rightConeSideDirection = Quaternion.Euler(0, 0, maxAngle / 2) * -transform.up;
        //Gizmos.DrawRay(transform.position + _center, leftConeSideDirection * primarySurfaceDistance);
        //Gizmos.DrawRay(transform.position + _center, rightConeSideDirection * primarySurfaceDistance);

        Gizmos.DrawWireSphere(transform.position + _center, maxSurfaceDistance);

        Gizmos.color = Color.red;

        if (surface) {
            Gizmos.DrawWireSphere(surface.position, 0.25f);
            Debug.DrawRay(surface.position, surface.normal, Color.red);
        }

        if (!Application.isPlaying) { _center = transform.up * yOffset; }
    }

    
    public class SurfacePoint {
        public Vector2 position;
        public Vector2 normal;

        public RaycastHit2D point;

        SurfacePoint(RaycastHit2D point) {
            this.point = point;

            this.position = point.point;
            this.normal = point.normal;
        }

        public float DistanceTo(Vector3 point) {
            return Vector3.Distance(point, position);
        }

        public static implicit operator SurfacePoint(RaycastHit2D rhp) {
            return new SurfacePoint(rhp);
        }

        public static implicit operator bool(SurfacePoint sp) {
            if (ReferenceEquals(sp, null)) { return false; }
            if (!sp.point) { return false; }
            else { return true; }
        }
    }
}
