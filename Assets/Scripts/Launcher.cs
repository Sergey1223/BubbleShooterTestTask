using System.Collections;
using UnityEngine;

public class Launcher : MonoBehaviour
{
    private const string TRAJECTORIES_CONTAINER_OBJECT_NAME = "Trajectories Container";

    public delegate void FiredHandler(GameObject bubble);
    public event FiredHandler Fired;

    [Tooltip("Maximum deflection angle from plumb line.")]
    [Range(0, 90)]
    public float angleLimit;

    [Tooltip("Minimum distance between anchor and bubble.")]
    [Range(0.1f, 4.9f)]
    public float lowerStrokeLimit;

    [Tooltip("Maximum distance between anchor and bubble.")]
    [Range(0.2f, 5)]
    public float upperStrokeLimit;

    [Tooltip("Maximum impulse.")]
    [Range(10, 30)]
    public float maximumImpulse;

    [Tooltip("Left flying bound.")]
    public GameObject leftBorder;

    [Tooltip("Right flying bound.")]
    public GameObject rightBorder;

    [Tooltip("Top flying bound.")]
    public GameObject topBorder;

    [Tooltip("Bottom flying bound.")]
    public GameObject bottomBorder;

    [Tooltip("Game orchestrator.")]
    public GameObject orchestrator;

    [Tooltip("Trajectory point prefab.")]
    public GameObject trajectoryPoint;

    [Tooltip("Color for single trajectory.")]
    public Color trajectoryColor;

    [Tooltip("Color for single trajectory.")]
    public Color doubleTrajectoryColor;

    [Tooltip("Spread angle in degrees.")]
    [Range(1, 15)]
    public float spreadAngle;

    [Tooltip("Time stamp between trajectory points.")]
    [Range(0.01f, 0.5f)]
    public float timeStamp;

    [Tooltip("Maximum count of sub trajectories for one shot.")]
    [Range(1, 5)]
    public int trajectoriesCount;

    private GameObject rail;
    private GameObject claim;
    private GameObject powerIndicator;
    private GameObject trajectoriesContainer;
    private GameObject loadedBubble;

    private Vector3 anchorPosition;
    private Vector3 railStartPosition;
    private Quaternion railStartRotation;
    private Vector3 fireDirection;
    private float? leftBorderPosition = null;
    private float? rightBorderPosition = null;
    private float power;

    // Start is called before the first frame update
    private void Start()
    {
        if (lowerStrokeLimit > upperStrokeLimit)
        {
            Debug.LogWarning("Lower stroke limit more than upper.");
        }

        rail = transform.Find("Rail").gameObject;
        railStartPosition = rail.transform.position;
        railStartRotation = rail.transform.rotation;

        claim = transform.Find("Claim").gameObject;

        powerIndicator = transform.Find("PowerIndicator").gameObject;

        anchorPosition = transform.position;
    }


    internal void LoadBubble(GameObject bubble)
    {
        if (!leftBorderPosition.HasValue)
        {
            float bubbleRadius = bubble.GetComponent<CircleCollider2D>().radius;

            leftBorderPosition = leftBorder.transform.position.x + leftBorder.GetComponent<BoxCollider2D>().size.x / 2 + bubbleRadius;
            rightBorderPosition = rightBorder.transform.position.x - rightBorder.GetComponent<BoxCollider2D>().size.x / 2 - bubbleRadius;
        }

        bubble.GetComponent<Rigidbody2D>().gravityScale = 0;

        claim.GetComponent<Claim>().Push(bubble);
    }

    internal void Reload(float delay)
    {
        StartCoroutine(Reloading(delay));
    }

    private IEnumerator Reloading(float delay)
    {
        yield return new WaitForSeconds(delay);

        ResetPosition();

        loadedBubble = claim.GetComponent<Claim>().Pop();

        if (loadedBubble == null)
        {
            yield break;
        }
        
        Bubble bubbleComponent = loadedBubble.GetComponent<Bubble>();

        loadedBubble.transform.parent = rail.transform;
        loadedBubble.transform.localPosition = new Vector3(0, -lowerStrokeLimit, 0);
        bubbleComponent.PositionChanged += UpdatePosition;
        bubbleComponent.PositionFixed += Fire;
        bubbleComponent.IsTouchable = true;

        powerIndicator.GetComponent<PowerIndicator>().UpdateValue(lowerStrokeLimit / upperStrokeLimit);
    }

    private void ResetPosition()
    {
        rail.transform.position = railStartPosition;
        rail.transform.rotation = railStartRotation;
    }

    private void UpdatePosition(Vector3 newPosition)
    {
        Vector3 deflection = newPosition - anchorPosition;
        float magnitude = deflection.magnitude;
        float deflectionAngle;

        // III or IV quarter
        if (newPosition.y < anchorPosition.y)
        {
            deflectionAngle = Vector3.Angle(-Vector3.up, deflection);
        }
        // I or II quarter
        else
        {
            deflectionAngle = Vector3.Angle(Vector3.up, deflection);
        }

        if (deflectionAngle <= angleLimit)
        {
            // Determination half (left (II + III quarters) or right (i + IV quarters-))
            float direction = Mathf.Sign(newPosition.x - anchorPosition.x);

            rail.transform.rotation = Quaternion.Euler(0, 0, direction * deflectionAngle);

            fireDirection = Quaternion.Euler(0, 0, direction * deflectionAngle) * Vector3.up;

            // I or II quarter
            if (newPosition.y > anchorPosition.y)
            {
                magnitude = lowerStrokeLimit;
            }
        }
        else
        {
            // III or IV quarter
            if (newPosition.y < anchorPosition.y)
            {
                magnitude = Mathf.Cos(Mathf.Deg2Rad * (deflectionAngle - angleLimit)) * magnitude;
            }
            // I or II quarter
            else
            {
                magnitude = Mathf.Cos(Mathf.Deg2Rad * (180 - angleLimit - deflectionAngle)) * magnitude;
            }
        }

        // Limiting magnitude if needed
        if (magnitude < lowerStrokeLimit)
        {
            magnitude = lowerStrokeLimit;
        }
        if (magnitude > upperStrokeLimit)
        {
            magnitude = upperStrokeLimit;
        }

        Vector3 newLocalPosition = new Vector3(0, -magnitude, 0);
        loadedBubble.transform.localPosition = newLocalPosition;

        // Update power
        float newPower = magnitude / upperStrokeLimit;
        if (newPower != power)
        {
            power = newPower;

            powerIndicator.GetComponent<PowerIndicator>().UpdateValue(power);
        }

        UpdateTrajectories();
    }

    private void UpdateTrajectories()
    {
        Destroy(trajectoriesContainer);
        trajectoriesContainer = new GameObject(TRAJECTORIES_CONTAINER_OBJECT_NAME);
        trajectoriesContainer.transform.parent = gameObject.transform;

        float speed = power * maximumImpulse / loadedBubble.GetComponent<Rigidbody2D>().mass;
        float angle = Mathf.Sign(fireDirection.y) * Vector3.Angle(Vector3.right, fireDirection);
        int level = trajectoriesCount;

        if (power == 1)
        {
            BuildTrajectory(loadedBubble.transform.position, speed, angle + spreadAngle, level, doubleTrajectoryColor);

            BuildTrajectory(loadedBubble.transform.position, speed, angle - spreadAngle, level, doubleTrajectoryColor);
        }
        else
        {
            BuildTrajectory(loadedBubble.transform.position, speed, angle, level, trajectoryColor);
        }
    }

    private void BuildTrajectory(Vector3 source, float startSpeed, float startAngle, int level, Color color)
    {
        if (level <= 0)
        {
            return;
        }

        Orchestartor orchestartorComponent = orchestrator.GetComponent<Orchestartor>();

        int startRow = orchestrator.GetComponent<Orchestartor>().FindCellPosition(source).row;
        if (startRow > orchestartorComponent.GridHeight - 1)
        {
            startRow = orchestartorComponent.GridHeight - 1;
        }

        Vector3 intersectionPoint = Vector2.zero;
        float time;
        float maxHeight = MathUtil.MaxHeight(startSpeed, startAngle);

        if (startAngle > 0)
        {
            // Check for direct collision with any bubble.
            for (int i = startRow; i >= 0; i--)
            {
                intersectionPoint = orchestrator.GetComponent<Orchestartor>().FindCellAnchorPosition((i, 0));

                // Max flight height point located abowe then current row height and betweeen side bounds.
                if (source.y + maxHeight >= intersectionPoint.y)
                {
                    time = MathUtil.AscentTime(startSpeed, startAngle, intersectionPoint.y - source.y, false);
                    intersectionPoint.x = source.x + MathUtil.XPosition(startSpeed, startAngle, time);

                    // Building trajectory to first bubble.
                    if (intersectionPoint.x > leftBorderPosition && intersectionPoint.x < rightBorderPosition && !orchestrator.GetComponent<Orchestartor>().IsEmpty(intersectionPoint))
                    {
                        BuildTrajectoryPoints(source, startSpeed, startAngle, time, color);

                        //Debug.Log(source.y + maxHeight);


                        return;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        if (source.y + maxHeight > topBorder.transform.position.y)
        {
            time = MathUtil.AscentTime(startSpeed, startAngle, topBorder.transform.position.y - source.y, false);

            // Building trajectory to top border.
            BuildTrajectoryPoints(source, startSpeed, startAngle, time, color);

            return;
        }

        if (Mathf.Abs(startAngle) > 90)
        {
            intersectionPoint.x = leftBorderPosition.Value;
        }
        else
        {
            intersectionPoint.x = rightBorderPosition.Value;
        }

        time = (intersectionPoint.x - source.x) / MathUtil.SpeedXProjection(startSpeed, startAngle);
        intersectionPoint.y = source.y + MathUtil.YPosition(startSpeed, startAngle, time);

        if (intersectionPoint.y < bottomBorder.transform.position.y)
        {
            time = MathUtil.AscentTime(startSpeed, startAngle, bottomBorder.transform.position.y - source.y, true);

            // Building trajectory to bottm border.
            BuildTrajectoryPoints(source, startSpeed, startAngle, time, color);

            return;
        }

        // Building trajectory to point of intersection trajectory with side border.
        BuildTrajectoryPoints(source, startSpeed, startAngle, time, color);

        // Building new reflected trajectory.
        Vector2 surfaceNormal = -Mathf.Sign(intersectionPoint.x) * Vector2.right;
        float newAngle = Bubble.ReflectTrajectory(startSpeed, startAngle, time, surfaceNormal);
        float newSpeed = MathUtil.CurrentSpeed(startSpeed, startAngle, time);
        BuildTrajectory(intersectionPoint, newSpeed, newAngle, --level, color);

        return;
    }

    private void BuildTrajectoryPoints(Vector3 source, float speed, float angle, float time, Color color)
    {
        while (time > 0)
        {
            Vector2 position = new Vector2(MathUtil.XPosition(speed, angle, time) + source.x, MathUtil.YPosition(speed, angle, time) + source.y);

            GameObject point = Instantiate(trajectoryPoint, position, Quaternion.identity, trajectoriesContainer.transform);
            point.GetComponent<SpriteRenderer>().color = color;

            time -= timeStamp;
        }
    }

    private void Fire()
    {
        float speed = power * maximumImpulse / loadedBubble.GetComponent<Rigidbody2D>().mass;
        
        float angle = Mathf.Sign(fireDirection.y) * Vector3.Angle(Vector3.right, fireDirection);
        if (power == 1)
        {
            angle = Random.Range(angle - spreadAngle, angle + spreadAngle);
        }

        loadedBubble.transform.parent = null;

        Bubble bubble = loadedBubble.GetComponent<Bubble>();
        bubble.PositionChanged -= UpdatePosition;
        bubble.PositionFixed -= Fire;
        bubble.IsTouchable = false;

        if (power == 1)
        {
            bubble.HasMaxImpulse = true;
        }

        bubble.ApplyImpulse(speed, angle);

        Destroy(trajectoriesContainer);

        Fired(loadedBubble);
    }
}
