using System.Collections;
using UnityEngine;

public class Launcher : MonoBehaviour
{
    private const string TRAJECTORIES_CONTAINER_OBJECT_NAME = "Trajectories Container";

    public delegate void PowerChangedHandler(float newPower);
    public event PowerChangedHandler PowerChanged;

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

    [Tooltip("Bottom flying bound.")]
    public GameObject bottomBorder;

    [Tooltip("Game orchestrator.")]
    public GameObject orchestrator;

    [Tooltip("Trajectory point prefab.")]
    public GameObject trajectoryPoint;

    //[Tooltip("Container for all trajectory points.")]


    [Tooltip("Spread angle in degrees.")]
    [Range(1, 15)]
    public float spreadAngle;

    [Tooltip("Time stamp between trajectory points.")]
    [Range(0.01f, 0.5f)]
    public float timeStamp;

    private GameObject rail;
    private GameObject claim;
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
        Init();
    }

    // Update is called once per frame
    private void Update()
    {
    }

    public void LoadBubble(GameObject bubble)
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

    public void Reload()
    {
        StartCoroutine(ReloadCoroutine());
    }

    public IEnumerator ReloadCoroutine()
    {
        yield return new WaitForSeconds(0.5f);

        resetPosition();

        loadedBubble = claim.GetComponent<Claim>().Pop();

        if (loadedBubble != null)
        {
            Bubble bubbleComponent = loadedBubble.GetComponent<Bubble>();

            loadedBubble.transform.parent = rail.transform;
            loadedBubble.transform.localPosition = new Vector3(0, -lowerStrokeLimit, 0);
            bubbleComponent.PositionChanged += UpdatePosition;
            bubbleComponent.PositionFixed += Fire;
            bubbleComponent.IsTouchable = true;
        }

        yield break;
    }

    public void resetPosition()
    {
        rail.transform.position = railStartPosition;
        rail.transform.rotation = railStartRotation;
    }

    private void Init()
    {
        if (lowerStrokeLimit > upperStrokeLimit)
        {
            Debug.LogWarning("Lower stroke limit more than upper.");
        }

        rail = transform.Find("Rail").gameObject;
        railStartPosition = rail.transform.position;
        railStartRotation = rail.transform.rotation;

        claim = transform.Find("Claim").gameObject;

        anchorPosition = transform.position;
    }

    private void UpdatePosition(Vector3 newPosition)
    {
        Vector3 deflection = newPosition - anchorPosition;

        //fireDirection = -deflection.normalized;

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

            // I or II quarter
            if (newPosition.y > anchorPosition.y)
            {
                magnitude = lowerStrokeLimit;
            }
            // III or IV quarter
            else
            {
                // Remember valid fire direction.
                fireDirection = -deflection.normalized;
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

            PowerChanged?.Invoke(power);
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

        BuildTrajectory(loadedBubble.transform.position, speed, angle);
    }

    //private void BuildTrajectory(Vector3 source, float startSpeed, float startAngle)
    //{
    //    Orchestartor orchestartorComponent = orchestrator.GetComponent<Orchestartor>();

    //    //Vector3 sidePoint = Vector3.zero;
    //    int offset = (int)Mathf.Sign(fireDirection.x);
    //    int startRow = orchestrator.GetComponent<Orchestartor>().FindCellPosition(source).row;

    //    if (startRow > orchestartorComponent.GridHeight - 1)
    //    {
    //        startRow = orchestartorComponent.GridHeight - 1;
    //    }

    //    for (int i = startRow; i >= 0; i--)
    //    {
    //        Vector3 point = orchestrator.GetComponent<Orchestartor>().FindCellAnchorPosition((i, 0));
    //        float maxHeight = MathUtil.MaxHeight(startSpeed, startAngle);
    //        float time;

    //        // Max flight height less then current row height. For this case builds full trajectory.
    //        if (source.y + maxHeight < point.y)
    //        {
    //            if (point.x > leftBound.transform.position.x && point.x < rightBound.transform.position.x)
    //            {
    //                time = MathUtil.AscentTime(
    //                startSpeed,
    //                startAngle,
    //                bottomBound.transform.position.y - source.y, true);

    //                BuildTrajectoryPoints(startSpeed, startAngle, time);

    //                return;
    //            }
    //            else
    //            {
    //                if (point.x <= leftBound.transform.position.x)
    //                {
    //                    // Time at wich will occur crossing trajectory and left bound.
    //                    time = (source.x - leftBound.transform.position.x) / MathUtil.SpeedXProjection(startSpeed, startAngle);

    //                    point.y = source.y + MathUtil.YPosition(startSpeed, startAngle, time);
    //                    BuildTrajectoryPoints(startSpeed, startAngle, time);

    //                    // Building new reflected trajectory.
    //                    float newAngle = Bubble.ReflectTrajectory(startSpeed, startAngle, time);
    //                    float newSpeed = MathUtil.CurrentSpeed(startSpeed, startAngle, time);
    //                    BuildTrajectory(point, newSpeed, newAngle);

    //                    return;
    //                }
    //            }

    //        }

    //        time = MathUtil.AscentTime(startSpeed, startAngle, point.y - source.y, false);
    //        point.x = source.x + MathUtil.XPosition(startSpeed, startAngle, time);

    //        // Max flight height point located betweeen side bounds. For this case builds trajectory to first bubble.
    //        if (point.x > leftBound.transform.position.x && point.x < rightBound.transform.position.x)
    //            {
    //            if (!orchestrator.GetComponent<Orchestartor>().IsEmpty(point))
    //            {
    //                BuildTrajectoryPoints(startSpeed, startAngle, time);

    //                return;
    //            }
    //        }

    //        // Max flight height point located beyond outer borders. For this case builds trajectory to border.
    //        else
    //        {
    //            if (point.x <= leftBound.transform.position.x)
    //            {
    //                // Time at wich will occur crossing trajectory and left bound.
    //                time = (source.x - leftBound.transform.position.x) / MathUtil.SpeedXProjection(startSpeed, startAngle);

    //                point.y = source.y + MathUtil.YPosition(startSpeed, startAngle, time);
    //                BuildTrajectoryPoints(startSpeed, startAngle, time);

    //                // Building new reflected trajectory.
    //                float newAngle = Bubble.ReflectTrajectory(startSpeed, startAngle, time);
    //                float newSpeed = MathUtil.CurrentSpeed(startSpeed, startAngle, time);
    //                BuildTrajectory(point, newSpeed, newAngle);

    //                return;
    //            }
    //            else//if (point.x >= rightBound.transform.position.x)
    //            {
    //                // Time at wich will occur crossing trajectory and right bound.
    //                //time = transform.position.x / MathUtil.SpeedXProjection(startSpeed, startAngle);

    //                //BuildTrajectoryPoints(startSpeed, startAngle, time);
    //                //return;
    //            }
    //        }
    //    }
    //}

    private void BuildTrajectory(Vector3 source, float startSpeed, float startAngle)
    {
        Orchestartor orchestartorComponent = orchestrator.GetComponent<Orchestartor>();

        int startRow = orchestrator.GetComponent<Orchestartor>().FindCellPosition(source).row;
        if (startRow > orchestartorComponent.GridHeight - 1)
        {
            startRow = orchestartorComponent.GridHeight - 1;
        }

        Vector3 intersectionPoint = Vector2.zero;// = orchestrator.GetComponent<Orchestartor>().FindCellAnchorPosition((i, 0));
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
                        BuildTrajectoryPoints(source, startSpeed, startAngle, time);

                        return;
                    }
                }
                else
                {
                    break;
                }
            }
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

        // Intersection point located below then bottom borser.
        if (intersectionPoint.y < bottomBorder.transform.position.y)
        {
            // Flying time to bottom border.
            time = MathUtil.AscentTime(startSpeed, startAngle, bottomBorder.transform.position.y - source.y, true);

            BuildTrajectoryPoints(source, startSpeed, startAngle, time);

            return;
        }

        // Debug.Log("X: " + intersectionPoint.x +"; Y: " + intersectionPoint.y);

        // Building trajectory to point of intersection trajectory with side border.
        BuildTrajectoryPoints(source, startSpeed, startAngle, time);

        // Building new reflected trajectory.
        // intersectionPoint.y += source.y;
        float newAngle = Bubble.ReflectTrajectory(startSpeed, startAngle, time);
        float newSpeed = MathUtil.CurrentSpeed(startSpeed, startAngle, time);
        //intersectionPoint.x 
        BuildTrajectory(intersectionPoint, newSpeed, newAngle);

        return;
    }

    private void BuildTrajectoryPoints(Vector3 source, float speed, float angle, float time)
    {
        //int pointsCount = Mathf.RoundToInt(time / timeStamp);

        //for (int i = 0; i < pointsCount; i++)
        //{
        //    Instantiate(trajectoryPoint, new Vector3(MathUtil.XPosition(speed, angle, time)), Quaternion.identity);
        //}

        while (time > 0)
        {
            Vector2 position = new Vector2(
                MathUtil.XPosition(speed, angle, time) + source.x,// loadedBubble.transform.position.x,
                MathUtil.YPosition(speed, angle, time) + source.y);

            //point.y -= loadedBubble.transform.position.y;


            Instantiate(trajectoryPoint, position, Quaternion.identity, trajectoriesContainer.transform);

            time -= timeStamp;
        }
    }

    private void Fire()
    {
        //if (power != 0f)
        //{
        //float angle = rail.transform.eulerAngles.z;


        //if (angle > 90)
        //{
        //    angle = 90 - (360 - angle);
        //}
        //else
        //{
        //    angle -= 90;
        //}

        float speed = power * maximumImpulse / loadedBubble.GetComponent<Rigidbody2D>().mass;
        //directionByXAxis = Mathf.Sign(angle);
        // startAngle = Mathf.Abs(angle);
        float angle = Mathf.Sign(fireDirection.y) * Vector3.Angle(Vector3.right, fireDirection);

        loadedBubble.transform.parent = null;

        Bubble bubble = loadedBubble.GetComponent<Bubble>();
        bubble.PositionChanged -= UpdatePosition;
        bubble.PositionFixed -= Fire;
        bubble.IsTouchable = false;
        bubble.ApplyImpulse(fireDirection, speed, angle);//power * maximumImpulse);
        //}

        //Reload();

        // TODo: Disabling traectories.
        Destroy(trajectoriesContainer);//.SetActive(false);

        Fired(loadedBubble);
    }

    private void ChangeAngle(Vector3 position)
    {
        //Debug.Log("PY: " + position.y);
        //Debug.Log("APY: " + anchorPosition.y);

        float deflectionAngle;
        if (position.y < anchorPosition.y)
        {
            deflectionAngle = Vector3.Angle(-Vector3.up, position - anchorPosition);
        }
        else
        {
            deflectionAngle = Vector3.Angle(Vector3.up, position - anchorPosition);
        }

        //Debug.Log("Deflection angle: " + deflectionAngle);
        //Debug.Log("Angle limit: " + angleLimit);

        if (deflectionAngle <= angleLimit)
        {
            float direction = Mathf.Sign(position.x - anchorPosition.x);

            // New rotation regarding blump line.
            rail.transform.rotation = Quaternion.Euler(0, 0, direction * deflectionAngle);
        }
    }

    private void DisplaceLoadedBubble(Vector3 newPosition)
    {
        Vector3 deflection = newPosition - anchorPosition;

        if (deflection.magnitude >= lowerStrokeLimit && deflection.magnitude <= upperStrokeLimit)
        {
            //Debug.Log("AP: " + anchorPosition);
            //Debug.Log("Magnitude: " + deflection.magnitude);
            //Debug.Log("NP: " + newPosition);
            //Debug.Log("Limit: " + lowerStrokeLimit);
            Vector3 newLocalPosition = new Vector3(0, -deflection.magnitude, 0);
            loadedBubble.transform.localPosition = newLocalPosition;

            float newPower = (upperStrokeLimit - lowerStrokeLimit) / newLocalPosition.magnitude * 100;

            if (newPower != power)
            {
                power = newPower;

                PowerChanged?.Invoke(power);
            }
        }
    }
}
