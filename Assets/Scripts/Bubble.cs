using System;
using System.Collections;
using UnityEngine;

class Bubble : MonoBehaviour
{
    private const float FREE_FALL_ACCELERATION = 9.8f;

    public delegate void PositionChangedHandler(Vector3 newPosition);
    public event PositionChangedHandler PositionChanged;

    public delegate void PositionFixedHandler();
    public event PositionFixedHandler PositionFixed;

    public delegate void OnBubbleCollisionHandler(GameObject projectile, GameObject collided);
    public event OnBubbleCollisionHandler OnBubbleCollision;

    public delegate void BurstedHandler();
    public event BurstedHandler Bursted;

    [Tooltip("Speed loss on collision with side edges (as a percetage).")]
    [Range(0, 100)]
    public float speedLoss;

    [HideInInspector]
    public bool IsTouchable { get; set; }

    [HideInInspector]
    public bool BorderCollisionEnabled { get; set; } = true;

    [HideInInspector]
    public (int row, int column) GridPosition { get; set; }

    private bool isTouched;
    private Coroutine flight;
    private Vector2 position;
    private Vector3 startPosition;
    private float startSpeed;
    private float startAngle;
    private float directionByXAxis;
    private float directionByYAxis = 1;
    private float flyingTime;


    private void Start()
    {

    }

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
        if (IsTouchable)
        {
            isTouched = true;
        }
    }

    private void OnMouseUp()
    {
        isTouched = false;

        PositionFixed?.Invoke();
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
            //Debug.Log("Bubble collision.");

            OnBubbleCollision?.Invoke(gameObject, collision.gameObject);
        }
    }

    public void ApplyImpulse(/*float angle*/ Vector3 direction, float speed, float angle)//float impulse)
    {
        startPosition = transform.position;
        startSpeed = speed;// impulse / GetComponent<Rigidbody2D>().mass;
        //directionByXAxis = Mathf.Sign(angle);
        // startAngle = Mathf.Abs(angle);
        directionByXAxis = Mathf.Sign(direction.x);
        startAngle = angle; // Mathf.Sign(direction.y) * Vector3.Angle(Vector3.right, direction);

        //if (directionByXAxis < 0)
        //{
        //    startAngle = Vector3.Angle(-Vector3.right, direction);
        //}
        //else
        //{
        //    startAngle = Vector3.Angle(Vector3.right, direction);
        //}
        // startAngle = Mathf.Abs(angle);

        flight = StartCoroutine(Fly());
    }

    public void Burst()
    {
        Bursted?.Invoke();

        StartCoroutine(BurstCoroutine());
    }

    /// <summary>
    /// Updates position on flying trajectory.
    /// X coordinate calculating using formula x = v * cos(a) * t, where v - start speed, a - angle between X axis and start direction, t - time from start.
    /// Y coordinate calculating using formula x = v * sin(a) * t - (g * t^2) / 2, where v - start speed, a - angle between X axis and start direction, t - time from start,
    /// g - acceleration of free fall.
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="impulse"></param>
    /// <returns></returns>
    private IEnumerator Fly()
    {
        flyingTime = 0;

        while (true)
        {
            float abscissa = /*directionByXAxis*/startSpeed * Mathf.Cos(Mathf.Deg2Rad * startAngle) * flyingTime;
            float ordinate = startSpeed * Mathf.Sin(Mathf.Deg2Rad * startAngle) * flyingTime - (FREE_FALL_ACCELERATION * flyingTime * flyingTime) / 2;
            transform.position = new Vector3(startPosition.x + abscissa, startPosition.y + ordinate, 0);

            //Debug.Log(transform.position.y);

            flyingTime += Time.deltaTime;

            yield return null;
        }
    }

    public IEnumerator BurstCoroutine()
    {
        GetComponent<Animator>().SetBool("Burst", true);

        yield return new WaitForSeconds(0.1f);

        Destroy(gameObject);
    }

    /// <summary>
    /// Processing collision with side bound.
    /// Angle calculating using formula β =  arctg(VY / VX), where VX - speed X projection, VY - speed Y projection.
    /// Current speed calculating using formula vc = sqrt(VX^2 + VY^2), where VX - speed X projection, VY - speed Y projection.
    /// </summary>
    /// <param name="boundPosition"></param>
    private void ProcessBoundsCollision(Collision2D collision)
    {
        if (collision.gameObject.name.Equals("BottomEdge"))
        {
            Burst();

            Debug.Log("Bursted");
        }
        else
        {
            float VX = CalculateSpeedVX();
            float VY = CalculateSpeedVY();
            float currentAngle = /*-directionByXAxis * Mathf.Abs(*/Mathf.Atan(VY / VX) * Mathf.Rad2Deg;//);
            Vector3 newAngle = Quaternion.Euler(0, 0, -currentAngle).eulerAngles.normalized;
            float currentSpeed = Mathf.Sqrt(VX * VX + VY * VY);

            Vector3 vector = new Vector3(VX, VY, 0).normalized;

            Vector3 result = Vector3.Reflect(new Vector3(VX, VY, 0).normalized, -Mathf.Sign(vector.x) * Vector3.right);

            float angle = Mathf.Sign(result.y) * Vector3.Angle(Vector3.right, result);

            ApplyImpulse(result, currentSpeed * GetComponent<Rigidbody2D>().mass, angle);

            Debug.Log("Border collsision with: " + collision.gameObject.name);
        }
    }

    /// <summary>
    /// Returns new angle after reflection.
    /// </summary>
    /// <param name="startSpeed">Start speed.</param>
    /// <param name="startAngle">Start angle between impulse direction and X axis.</param>
    /// <param name="flyingTime">Time from flight start.</param>
    /// <returns>New angle in degrees.</returns>
    internal static float ReflectTrajectory(float startSpeed, float startAngle, float flyingTime)
    {
        Vector2 speedVector = new Vector3(MathUtil.SpeedXProjection(startSpeed, startAngle), MathUtil.SpeedYProjection(startSpeed, startAngle, flyingTime));

        Vector3 reflectedSpeedVector = Vector3.Reflect(speedVector, -Mathf.Sign(speedVector.x) * Vector3.right);

        return Mathf.Sign(reflectedSpeedVector.y) * Vector3.Angle(Vector3.right, reflectedSpeedVector);
    }

    /// <summary>
    /// Calculating speed X projection using formula VX = v * cos(α)), where v - start speed, α - angle between X axis and start direction.
    /// </summary>
    private float CalculateSpeedVX()
    {
        return startSpeed * Mathf.Cos(Mathf.Deg2Rad * startAngle);
    }

    /// <summary>
    /// Calculating speed Y projection using formula VY = v * sin(α) - g * t, where v - start speed, α - angle between X axis and start direction, t - time from start,
    /// g - acceleration of free fall
    /// </summary>
    [Obsolete()]
    private float CalculateSpeedVY()
    {
        return startSpeed * Mathf.Sin(Mathf.Deg2Rad * startAngle) - FREE_FALL_ACCELERATION * flyingTime;
    }

}