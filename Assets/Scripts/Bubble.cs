using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

class Bubble : MonoBehaviour
{
    internal delegate void PositionChangedHandler(Vector3 newPosition);
    internal event PositionChangedHandler PositionChanged;

    internal delegate void PositionFixedHandler();
    internal event PositionFixedHandler PositionFixed;

    internal delegate void OnBubbleCollisionHandler(GameObject projectile, GameObject collided);
    internal event OnBubbleCollisionHandler OnBubbleCollision;

    internal delegate void BurstedHandler();
    internal event BurstedHandler Bursted;

    [HideInInspector]
    internal bool IsTouchable { get; set; }

    [HideInInspector]
    internal bool BorderCollisionEnabled { get; set; } = true;

    [HideInInspector]
    internal bool HasMaxImpulse { get; set; }

    [HideInInspector]
    internal (int row, int column) GridPosition { get; set; }

    private bool isTouched;
    private Coroutine flight;
    private Vector2 position;
    private Vector3 startPosition;
    private float startSpeed;
    private float startAngle;
    private float flyingTime;

    private void Update()
    {
        if (isTouched)
        {
            Vector2 newPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            if (newPosition != position)
            {
                position = newPosition;

                PositionChanged?.Invoke(newPosition);
            }
        }
    }

    private void OnMouseDown()
    {
        if (IsTouchable && !EventSystem.current.IsPointerOverGameObject())
        {
            isTouched = true;
        }
    }

    private void OnMouseUp()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            isTouched = false;

            PositionFixed?.Invoke();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (flight != null)
        {
            StopCoroutine(flight);
        }

        if (!CompareTag(collision.gameObject.tag))
        {
            if (BorderCollisionEnabled)
            {
                ProcessBoundsCollision(collision);
            }
        }
        else
        {
            OnBubbleCollision?.Invoke(gameObject, collision.gameObject);
        }
    }

    internal void ApplyImpulse(float speed, float angle)
    {
        startPosition = transform.position;
        startSpeed = speed;
        startAngle = angle;

        flight = StartCoroutine(Flying());
    }

    internal void Burst()
    {
        GetComponent<AudioSource>().Play();

        Bursted?.Invoke();

        StartCoroutine(Bursting());
    }

    private IEnumerator Bursting()
    {
        GetComponent<Animator>().SetBool("Burst", true);

        yield return new WaitForSeconds(0.1f);

        Destroy(gameObject);
    }

    private IEnumerator Flying()
    {
        flyingTime = 0;

        while (true)
        {
            float abscissa = MathUtil.XPosition(startSpeed, startAngle, flyingTime);
            float ordinate = MathUtil.YPosition(startSpeed, startAngle, flyingTime);
            
            transform.position = new Vector3(startPosition.x + abscissa, startPosition.y + ordinate, 0);
            flyingTime += Time.deltaTime;

            yield return null;
        }
    }

    private void ProcessBoundsCollision(Collision2D collision)
    {
        if (collision.gameObject.name.Equals("BottomEdge"))
        {
            Burst();
        }
        else
        {
            float speedXProjection = MathUtil.SpeedXProjection(startSpeed, startAngle);
            float speedYProjection = MathUtil.SpeedYProjection(startSpeed, startAngle, flyingTime);
            float currentSpeed = Mathf.Sqrt(speedXProjection * speedXProjection + speedYProjection * speedYProjection);

            Vector2 surfaceNormal;

            if (collision.gameObject.name.Equals("TopEdge"))
            {
                surfaceNormal = -Vector2.up;
            }
            else
            {
                surfaceNormal = -Mathf.Sign(speedXProjection) * Vector2.right;
            }

            ApplyImpulse(currentSpeed, ReflectTrajectory(startSpeed, startAngle, flyingTime, surfaceNormal));
        }
    }

    /// <summary>
    /// Returns new angle after reflection.
    /// </summary>
    /// <param name="startSpeed">Start speed.</param>
    /// <param name="startAngle">Start angle between impulse direction and X axis.</param>
    /// <param name="flyingTime">Time from flight start.</param>
    /// <returns>New angle in degrees.</returns>
    internal static float ReflectTrajectory(float startSpeed, float startAngle, float flyingTime, Vector3 surfaceNormal)
    {
        Vector2 speedVector = new Vector3(MathUtil.SpeedXProjection(startSpeed, startAngle), MathUtil.SpeedYProjection(startSpeed, startAngle, flyingTime));
        Vector3 reflectedSpeedVector = Vector3.Reflect(speedVector, surfaceNormal);

        return Mathf.Sign(reflectedSpeedVector.y) * Vector3.Angle(Vector3.right, reflectedSpeedVector);
    }
}