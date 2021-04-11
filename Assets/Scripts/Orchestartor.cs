using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

class Orchestartor : MonoBehaviour
{
    // Directions for checking matches regarding pivot element with indexes (0, 0) in "short" row. Up, up + right, right, down + right, down, left. 
    private readonly (int row, int column)[] MACTH_DIRECTIONS_IN_SHORT_ROW = { (-1, 0), (-1, 1), (0, 1), (1, 1), (1, 0), (0, -1) };

    // Directions for checking matches regarding pivot element with indexes (0, 0) in "long" row. Up, right, down, down + left, left, up + left. 
    private readonly (int row, int column)[] MACTH_DIRECTIONS_IN_LONG_ROW = { (-1, 0), (0, 1), (1, 0), (1, -1), (0, -1), (-1, -1) };
    private readonly LinkDirection[] availableLinkDirections = {
        LinkDirection.UP_LEFT,
        LinkDirection.UP_RIGHT,
        LinkDirection.RIGHT,
        LinkDirection.DOWN_RIGHT,
        LinkDirection.DOWN_LEFT,
        LinkDirection.LEFT
    };

    [Tooltip("Bubble prefabs wich will be creating.")]
    public GameObject[] availableBubbles;

    [Range(5, 50)]
    [Tooltip("Bubble count horizontally.")]
    public int width;

    [Range(5, 50)]
    [Tooltip("Bubble count vertically.")]
    public int height;

    [Tooltip("Maximum bubble count vertically.")]
    public int maxHeight;

    [Tooltip("Distance between bubbles centers.")]
    public float interval;

    [Tooltip("Minimum bubbles count for fulfilling match condition.")]
    public int matchCount;

    [Tooltip("Minimum empty cell in first row (as a percentage) for win.")]
    [Range(0, 100)]
    public float emptyCellsPercentage;

    [Tooltip("Bubbles count which available for gamer.")]
    [Range(0, 30)]
    public int bubblesCount;

    [Tooltip("UI element that will render remaining bubbles counter.")]
    public GameObject bubblesCounter;

    [HideInInspector]
    internal int GridWidth { get { return width; } }

    [HideInInspector]
    internal int GridHeight { get { return maxHeight; } }

    [HideInInspector]
    internal float GridInterval { get { return interval; } }

    private GameObject[,] grid;
    private GameObject launcher;


    private void Start()
    {
        launcher = GameObject.Find("Launcher");
        launcher.GetComponent<Launcher>().Fired += FocusOnBubble;
        launcher.GetComponent<Launcher>().LoadBubble(CreateBubble());

        PrepareNextStep();

        FillGrid();
    }

    private void Update()
    {

    }

    internal bool IsEmpty(Vector3 position)
    { 
        return IsEmpty(FindCellPosition(position));
    }

    public (int row, int column) FindCellPosition(Vector3 position)
    {
        int row = (int)Math.Ceiling((transform.position.y - position.y) / interval) - 1;
        int column = (int)Math.Ceiling((position.x - transform.position.x - (row % 2) * interval / 2) / interval) - 1;

        return (row, column);
    }

    internal Vector3 FindCellAnchorPosition((int row, int column) cell)
    {
        float startInterval = interval / 2 * (cell.row % 2);

        return new Vector3(transform.position.x + startInterval + cell.column * interval, transform.position.y - cell.row * interval);
    }

    private void PrepareNextStep()
    {
        launcher.GetComponent<Launcher>().LoadBubble(CreateBubble());
        launcher.GetComponent<Launcher>().Reload();
    }

    private void FillGrid()
    {
        if (maxHeight <= height)
        {
            Debug.LogError("Incorrect max height value.");
        }

        Vector2 pivotPosition = new Vector2(transform.position.x, transform.position.y);
        grid = new GameObject[maxHeight, width * 2];

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width - i % 2; j++)
            {
                GameObject bubble = CreateBubble(UnityEngine.Random.Range(0, availableBubbles.Length), pivotPosition);
                bubble.GetComponent<Bubble>().GridPosition = (i, j);

                grid[i, j] = bubble;
                // bubble.GetComponent<Rigidbody2D>().gravityScale = 0.5f;

                //SpringJoint2D springJoint = (SpringJoint2D) grid[i, j].AddComponent(typeof(SpringJoint2D));
                //springJoint.connectedAnchor = pivotPosition;
                //springJoint.frequency = 1;
                //springJoint.dampingRatio = 1;

                SpringJoint2D springJoint = bubble.GetComponent<SpringJoint2D>();
                springJoint.connectedAnchor = pivotPosition;
                springJoint.distance = 0;
                springJoint.enabled = true;

                pivotPosition.x += interval;
            }

            pivotPosition.x = transform.position.x + interval / 2 * ((i + 1) % 2);
            pivotPosition.y -= interval;
        }
    }

    private GameObject CreateBubble(/*bool useCounter*/)
    {
        //if (useCounter)
        //{
            bubblesCount--;
            bubblesCounter.GetComponent<Text>().text = bubblesCount.ToString();
        //}

        return CreateBubble(UnityEngine.Random.Range(0, availableBubbles.Length), new Vector3(0, 0, 1));
    }

    private GameObject CreateBubble(int variant, Vector3 position)
    {
        return Instantiate(availableBubbles[variant], position, Quaternion.identity);
    }

    

    private void FocusOnBubble(GameObject bubble)
    {
        bubble.GetComponent<Bubble>().OnBubbleCollision += ProcessShotResult;
        bubble.GetComponent<Bubble>().Bursted += PrepareNextStep;
    }

    private void UnfocusOnBubble(GameObject bubble)
    {
        bubble.GetComponent<Bubble>().OnBubbleCollision -= ProcessShotResult;
        bubble.GetComponent<Bubble>().Bursted -= PrepareNextStep;
    }

    private void ProcessShotResult(GameObject projectile, GameObject collided)
    {
        if (!TrySnap(projectile, collided))
        {
            // Game over
            transform.Find("GameOverPane").GetComponent<SpriteRenderer>().enabled = true;

            Debug.Log("Game Over");
        }
        else
        {
            CheckForMatch(projectile);
            CheckForHanging();

            if (bubblesCount == 0)
            {
                // Game over
                transform.Find("GameOverPane").GetComponent<SpriteRenderer>().enabled = true;

                Debug.Log("Game Over");
            }
            else
            {
                PrepareNextStep();
            }
        }
    }

    //private (int, int) SnapBubble(GameObject projectile, GameObject collided)
    //{
    //    int offsetByColumns = (int)Mathf.Sign(projectile.transform.position.x - collided.transform.position.x);
    //    int offsetByRows = (int)Mathf.Sign(projectile.transform.position.y - collided.transform.position.y);

    //    (int row, int column) collidedBubbleIndexes = FindCellIndexes(collided.GetComponent<SpringJoint2D>().connectedAnchor);

    //    (int row, int column) projectileBubbleIndexes = (collidedBubbleIndexes.row - offsetByRows, collidedBubbleIndexes.column + offsetByColumns + collidedBubbleIndexes.row % 2);

    //    grid[projectileBubbleIndexes.row, projectileBubbleIndexes.column] = projectile;

    //    SpringJoint2D springJoint = projectile.GetComponent<SpringJoint2D>();

    //    //SpringJoint2D springJoint = bubble.GetComponent<SpringJoint2D>();
    //    //springJoint.connectedAnchor = pivotPosition;
    //    //springJoint.enabled = true;

    //    // X component = reference point + start offset for even rows + column position; Y comonent = reference point - row position.
    //    springJoint.connectedAnchor = new Vector3(transform.position.x + (interval / 2 * ((collidedBubbleIndexes.row + 1) % 2)) + projectileBubbleIndexes.column * interval, transform.position.y - projectileBubbleIndexes.row * interval);
    //    springJoint.enabled = true;
    //    springJoint.distance = 0;

    //    projectile.GetComponent<Rigidbody2D>().gravityScale = 0.5f;

    //    UnfocusOnBubble(projectile);



    //    //int upperRowIndex =  Mathf.Abs((int) Math.Truncate((transform.position.y - projectile.transform.position.y) / interval));
    //    //int lowerRowIndex =  upperRowIndex + 1;
    //    //int leftColumnIndex =  Mathf.Abs((int) Math.Truncate((transform.position.x + interval * upperRowIndex % 2 - projectile.transform.position.x) / interval));
    //    //int rightColumnIndex =  leftColumnIndex + 1;

    //    //List<Vector3> vacantPlaces = new List<Vector3>(4);

    //    //if (grid[upperRowIndex, rightColumnIndex] == null)
    //    //{
    //    //    vacantPlaces.Add(new Vector3(transform.position.x + interval * rightColumnIndex, transform.position.y - interval * upperRowIndex));
    //    //}
    //    //if (grid[lowerRowIndex, rightColumnIndex] == null)
    //    //{
    //    //    vacantPlaces.Add(new Vector3(transform.position.x + interval * rightColumnIndex, transform.position.y - interval * lowerRowIndex));
    //    //}
    //    //if (grid[lowerRowIndex, leftColumnIndex] == null)
    //    //{
    //    //    vacantPlaces.Add(new Vector3(transform.position.x + interval * leftColumnIndex, transform.position.y - interval * lowerRowIndex));
    //    //}
    //    //if (grid[upperRowIndex, leftColumnIndex] == null)
    //    //{
    //    //    vacantPlaces.Add(new Vector3(transform.position.x + interval * leftColumnIndex, transform.position.y - interval * upperRowIndex));
    //    //}



    //    //// Finding vacant positions
    //    //float upperRowLongitude = transform.position.y - (float) Math.Truncate((transform.position.y - projectile.transform.position.y) / interval);
    //    //float lowerRow = upperRowLongitude - interval;
    //    //float leftRowLatitude = transform.position.x - (float) Math.Truncate((transform.position.x - projectile.transform.position.x) / interval);
    //    //float rightRowLatitude = upperRowLongitude + interval;

    //    //if (Mathf.Min(Vector3.Distance(projectile.transform.position, )))
    //    //{

    //    //}
    //    //if (collided.transform.position.x > projectile.transform.position.x)
    //    //{
    //    //    // 
    //    //}

    //    return projectileBubbleIndexes;
    //}

    //private bool TrySnap(GameObject projectile, GameObject target)
    //{
    //    Bubble targetBubble = target.GetComponent<Bubble>();

    //    int offsetByColumns = (int)Mathf.Sign(projectile.transform.position.x - target.transform.position.x);

    //    // Direction of rows index growth is opposite with respect to Y axis.
    //    int offsetByRows = (int)Mathf.Sign(target.transform.position.y - projectile.transform.position.y);

    //    // Buble can be placed only in two potential places. By default case it is left or right direction for first and up or down for second. 
    //    (int row, int column) first = (targetBubble.GridPosition.row, targetBubble.GridPosition.column + offsetByColumns);
    //    (int row, int column) second = (targetBubble.GridPosition.row + offsetByRows, targetBubble.GridPosition.column);

    //    // Uniq cases

    //    // For short row
    //    if (isShort(targetBubble.GridPosition.row))
    //    {
    //        if (offsetByColumns >= 0 && targetBubble.GridPosition.column < width - 1)
    //        {
    //            second.column += offsetByColumns;
    //        }
    //    }

    //    // For long row
    //    else
    //    {
    //        // Collision on left side
    //        if (offsetByColumns < 0)
    //        {
    //            // 
    //            if (targetBubble.GridPosition.column > 0)
    //            {
    //                second.column += offsetByColumns;
    //            }
    //        }
    //        // Collision on left side
    //        else
    //        {
    //            first = (targetBubble.GridPosition.row + offsetByRows * 2, targetBubble.GridPosition.column);
    //            second = (targetBubble.GridPosition.row + offsetByRows * 2, targetBubble.GridPosition.column);
    //        }
    //    }

    //    (int row, int column)? cell = FindNearestCell(projectile, out Vector3 position, first, second);

    //    if (cell != null)
    //    {
    //        grid[cell.Value.row, cell.Value.column] = projectile;
    //        projectile.GetComponent<Bubble>().GridPosition = cell.Value;
    //        projectile.GetComponent<Bubble>().BorderCollisionEnabled = false;

    //        SpringJoint2D springJoint = projectile.GetComponent<SpringJoint2D>();
    //        springJoint.connectedAnchor = position;
    //        springJoint.enabled = true;
    //        springJoint.distance = 0;

    //        projectile.GetComponent<Rigidbody2D>().gravityScale = 0.5f;

    //        UnfocusOnBubble(projectile);
    //    }

    //    return cell != null;
    //}

    private bool TrySnap(GameObject projectile, GameObject target)
    {
        List<(int, int)> availableCells = new List<(int, int)>(3);
        (int, int) cellPosition = target.GetComponent<Bubble>().GridPosition;

        // I or IV quarter
        if (projectile.transform.position.x >= target.transform.position.x)
        {
            availableCells.Add(GetAdjacentCellPosition(cellPosition, LinkDirection.RIGHT));

            // I quarter
            if (projectile.transform.position.y >= target.transform.position.y)
            {
                availableCells.Add(GetAdjacentCellPosition(cellPosition, LinkDirection.UP));
                availableCells.Add(GetAdjacentCellPosition(cellPosition, LinkDirection.UP_RIGHT));
            }
            // IV quarter
            else
            {
                availableCells.Add(GetAdjacentCellPosition(cellPosition, LinkDirection.DOWN_RIGHT));
                availableCells.Add(GetAdjacentCellPosition(cellPosition, LinkDirection.DOWN));
            }
        }
        // II or III quarter
        else
        {
            availableCells.Add(GetAdjacentCellPosition(cellPosition, LinkDirection.LEFT));

            // II quarter
            if (projectile.transform.position.y >= target.transform.position.y)
            {
                availableCells.Add(GetAdjacentCellPosition(cellPosition, LinkDirection.UP));
                availableCells.Add(GetAdjacentCellPosition(cellPosition, LinkDirection.UP_LEFT));
            }
            // III quarter
            else
            {
                availableCells.Add(GetAdjacentCellPosition(cellPosition, LinkDirection.DOWN_LEFT));
                availableCells.Add(GetAdjacentCellPosition(cellPosition, LinkDirection.DOWN));
            }
        }

        //Debug.Log("Target cell position: " + cellPosition);

        (int row, int column)? cell = FindEmptyCell(projectile, out Vector3 position, availableCells.ToArray());

        if (cell != null)
        {
            grid[cell.Value.row, cell.Value.column] = projectile;
            projectile.GetComponent<Bubble>().GridPosition = cell.Value;
            projectile.GetComponent<Bubble>().BorderCollisionEnabled = false;

            SpringJoint2D springJoint = projectile.GetComponent<SpringJoint2D>();
            springJoint.connectedAnchor = position;
            springJoint.enabled = true;
            springJoint.distance = 0;

            projectile.GetComponent<Rigidbody2D>().gravityScale = 0.5f;

            UnfocusOnBubble(projectile);
        }

        return cell != null;
    }

    private bool CheckForMatch(GameObject pivotBubble)
    {
        List<GameObject> matchedBubbles = new List<GameObject>() { pivotBubble };

        FindMatches(pivotBubble, matchedBubbles);


        if (matchedBubbles.Count >= matchCount)
        {
            foreach (GameObject bubble in matchedBubbles)
            {
                (int row, int column) = bubble.GetComponent<Bubble>().GridPosition;

                grid[row, column] = null;

                bubble.GetComponent<Bubble>().Burst();
            }

            return true;
        }

        return false;
    }

    private bool CheckForHanging()
    {
        List<GameObject> checkedCells = new List<GameObject>(width * maxHeight);
        bool result = true;

        for (int i = 1; i < maxHeight; i++)
        {
            for (int j = 0; j < width - i % 2; j++)
            {
                GameObject current = grid[i, j];

                if (current != null && !checkedCells.Contains(current))
                {
                    (int row, int column) currentCell = current.GetComponent<Bubble>().GridPosition;
                    (int row, int column) upLeftCell = GetAdjacentCellPosition(currentCell, LinkDirection.UP_LEFT);
                    (int row, int column) upRightCell = GetAdjacentCellPosition(currentCell, LinkDirection.UP_RIGHT);

                    if (IsEmpty(upLeftCell) && IsEmpty(upRightCell))
                    {
                        List<GameObject> buffer = new List<GameObject>() { current };

                        if (FindHanging(current, buffer))
                        {
                            checkedCells.AddRange(buffer);

                            buffer.Clear();
                        }
                        else
                        {
                            result = false;

                            foreach (GameObject bubble in buffer)
                            {
                                (int row, int column) = bubble.GetComponent<Bubble>().GridPosition;
                                grid[row, column] = null;

                                bubble.GetComponent<SpringJoint2D>().enabled = false;
                            }
                        }
                    }
                    else
                    {
                        checkedCells.Add(GetBubble(currentCell));
                    }
                }
            }
        }

        return result;
    }

    private bool CheckForWin()
    {
        // Counting empty cells.
        int emptyCells = 0;
        for (int i = 0; i < width; i++)
        {
            if (grid[0, i] != null)
            {
                emptyCells++;
            }
        }

        return emptyCells / width * 100 < emptyCellsPercentage;
    }

    private void FindMatches(GameObject bubble, List<GameObject> resultContainer)
    {
        (int row, int column) gridPosition = bubble.GetComponent<Bubble>().GridPosition;

        foreach ((int i, int j) in (isShort(bubble.GetComponent<Bubble>().GridPosition.row) ? MACTH_DIRECTIONS_IN_SHORT_ROW : MACTH_DIRECTIONS_IN_LONG_ROW))
        {
            if (gridPosition.row + i >= 0 && gridPosition.row + i < maxHeight && gridPosition.column + j >= 0 && gridPosition.column + j < width)
            {
                GameObject current = grid[gridPosition.row + i, gridPosition.column + j];
                if (current?.name == bubble.name && !resultContainer.Contains(current))
                {
                    resultContainer.Add(current);

                    FindMatches(current, resultContainer);
                }
            }
        }
    }

    private bool FindHanging(GameObject source, List<GameObject> resulContainer)
    {
        return FindHanging(source.GetComponent<Bubble>().GridPosition, resulContainer);
    }

    private bool FindHanging((int, int) cellPosition, List<GameObject> resulContainer)
    {
        foreach (LinkDirection direction in availableLinkDirections)
        {
            (int row, int column) currentCell = GetAdjacentCellPosition(cellPosition, direction);
            GameObject currentBubble = GetBubble(currentCell);

            if (currentBubble != null && !resulContainer.Contains(currentBubble))
            {
                if (currentCell.row == 0)
                {
                    return true;
                }
                else
                {
                    resulContainer.Add(currentBubble);

                    return FindHanging(currentCell, resulContainer);
                }
            }
        }

        // Debug.Log("Incorrect available direction.");

        return false;
    }

    /// <summary>
    /// Checks row for parity. Every row with not even index is "short" (has one less bubble).
    /// </summary>
    /// <param name=""></param>
    private bool isShort(int rowIndex)
    {
        return rowIndex % 2 == 1;
    }

    private (int, int)? FindEmptyCell(GameObject pivot, out Vector3 position, params (int, int)[] cellPositions)
    {
        (int, int)? result = null;
        position = Vector3.zero;
        float min = float.MaxValue;

        foreach ((int row, int column) cellPosition in cellPositions)
        {
            if (IsEmpty(cellPosition))
            {
                Vector3 newPosition = FindCellAnchorPosition(cellPosition);
                float distance = Vector3.Distance(pivot.transform.position, newPosition);

                //Debug.Log("Disnatce to" + cellPosition + ": " + distance);

                if (distance < min)
                {
                    min = distance;

                    result = cellPosition;
                    position = newPosition;
                }
            }
        }

        //Debug.Log("Min distance: " + min);

        return result;
    }

    private GameObject GetBubble((int row, int column) cellPosition)
    {
        if (CheckBounds(cellPosition))
        {
            return grid[cellPosition.row, cellPosition.column];
        }

        return null;
    }

    private bool CheckBounds((int row, int column) cellPosition)
    {
        return
            cellPosition.row >= 0 &&
            cellPosition.row < maxHeight &&
            cellPosition.column >= 0 &&
            cellPosition.column < width - cellPosition.row % 2;
    }

    internal bool IsEmpty((int row, int column) cellPosition)
    {
        if (CheckBounds(cellPosition))
        {
            return grid[cellPosition.row, cellPosition.column] == null;
        }

        // TODO: check on error
        return true;
    }

    private (int, int) GetAdjacentCellPosition((int row, int column) source, LinkDirection direction)
    {
        switch (direction)
        {
            case LinkDirection.UP:
                return (source.row - 2, source.column);
            case LinkDirection.UP_RIGHT:
                if (!isShort(source.row))
                {
                    return (source.row - 1, source.column);
                }

                return (source.row - 1, source.column + 1);
            case LinkDirection.RIGHT:
                return (source.row, source.column + 1);
            case LinkDirection.DOWN_RIGHT:
                if (!isShort(source.row))
                {
                    return (source.row + 1, source.column);
                }

                return (source.row + 1, source.column + 1);
            case LinkDirection.DOWN:
                return (source.row + 2, source.column);
            case LinkDirection.DOWN_LEFT:
                if (isShort(source.row))
                {
                    return (source.row + 1, source.column);
                }

                return (source.row + 1, source.column - 1);
            case LinkDirection.LEFT:
                return (source.row, source.column - 1);
            case LinkDirection.UP_LEFT:
                if (isShort(source.row))
                {
                    return (source.row - 1, source.column);
                }

                return (source.row - 1, source.column - 1);
            default:
                return source;
        }
    }
}

// Represent lin direction in grid. First two digits in value is a offset by rows, other - offse by columns.
enum LinkDirection : int
{
    UP,
    UP_RIGHT,
    RIGHT,
    DOWN_RIGHT,
    DOWN,
    DOWN_LEFT,
    LEFT,
    UP_LEFT
}

/// <summary>
/// Represents tools for calculating intermediate values the movement of a body thrown at an angle to the horizon.
/// </summary>
class MathUtil
{
    private const float FREE_FALL_ACCELERATION = 9.8f;

    /// Returns current X coordiante at the specified time.
    /// The following formula is used for calculation: x = v * sin(α) * t - (g * t^2) / 2, where v - start speed, a - angle between X axis and start direction,
    /// t - time from start, g - acceleration of free fall.
    /// </summary>
    /// <param name="startSpeed">Start speed.</param>
    /// <param name="startAngle">Start angle between impulse direction and X axis.</param>
    /// <param name="time">Current flying time.</param>
    /// <returns>Coordiante.</returns>
    public static float XPosition(float startSpeed, float startAngle, float time)
    {
        return startSpeed * Mathf.Cos(Mathf.Deg2Rad * startAngle) * time;
    }

    /// <summary>
    /// Returns time that is needed to ascent to specified height.
    /// The calculation of time is reduced to solving a quadratic equation with respect to specified Y (height).
    /// </summary>
    /// <param name="startSpeed">Start speed.</param>
    /// <param name="startAngle">Start angle between impulse direction and X axis.</param>
    /// <param name="height">Current flying height.</param>
    /// <param name="max">Flag that specifies which of two values to return.</param>
    /// <returns>Time in seconds.</returns>
    internal static float AscentTime(float startSpeed, float startAngle, float height, bool max)
    {
        float a = FREE_FALL_ACCELERATION;
        float b = -2 * startSpeed * Mathf.Sign(Mathf.Deg2Rad * startAngle);
        float c = 2 * height;
        float d = b * b - 4 * a * c;

        if (max)
        {
            return (-b + Mathf.Sqrt(d)) / (2 * a);
        }

        return (-b - Mathf.Sqrt(d)) / (2 * a);

        //return AscentTime(startSpeed, startAngle, height, 0);
    }

    /// <summary>
    /// Returns time that is needed to ascent to specified height.
    /// The calculation of time is reduced to solving a quadratic equation with respect to specified Y (height).
    /// </summary>
    /// <param name="startSpeed">Start speed.</param>
    /// <param name="startAngle">Start angle between impulse direction and X axis.</param>
    /// <param name="height">Current flying height.</param>
    /// <param name="startHeight">Height on flyight start.</param>
    /// <returns>Time in seconds.</returns>
    //public static float AscentTime(float startSpeed, float startAngle, float height, float startHeight)
    //{
    //    float a = FREE_FALL_ACCELERATION;
    //    float b = -2 * startSpeed * Mathf.Sign(Mathf.Deg2Rad * startAngle);
    //    float c = 2 * (height - startHeight);
    //    float d = b * b - 4 * a * c;

    //    return (-b + Mathf.Sqrt(d)) / (2 * a);
    //}

    /// <summary>
    /// Returns time that is needed to ascent to maximum height.
    /// The following formula is used for calculation: t = v * sin(α) / g, where v - start speed,
    /// α - angle between X axis and start direction, g - acceleration of free fall.
    /// </summary>
    /// <param name="startSpeed">Start speed.</param>
    /// <param name="startAngle">Start angle between impulse direction and X axis.</param>
    /// <returns>Time in seconds.</returns>
    internal static float FullAscentTime(float startSpeed, float startAngle)
    {
        return startSpeed * Mathf.Sin(Mathf.Deg2Rad * startAngle) / FREE_FALL_ACCELERATION;
    }

    /// <summary>
    /// Returns current Y coordiante at the specified time.
    /// The following formula is used for calculation: y = v * cos(α) * t, where v - start speed, a - angle between X axis and start direction, t - time from start.
    /// </summary>
    /// <param name="startSpeed">Start speed.</param>
    /// <param name="startAngle">Start angle between impulse direction and X axis.</param>
    /// <param name="time">Current flying time.</param>
    /// <returns>Coordiante.</returns>
    public static float YPosition(float startSpeed, float startAngle, float time)
    {
        return startSpeed * Mathf.Sin(Mathf.Deg2Rad * startAngle) * time - (FREE_FALL_ACCELERATION * time * time) / 2;
    }

    /// <summary>
    /// Returns projection of speed vector on X axis
    /// The following formula is used for calculation: VX = v * cos(α)), where v - start speed, α - angle between impulse direction and X axis.
    /// </summary>
    /// <param name="startSpeed">Start speed.</param>
    /// <param name="startAngle">Start angle between impulse direction and X axis.</param>
    /// <returns>Projection value.</returns>
    public static float SpeedXProjection(float startSpeed, float startAngle)
    {
        return startSpeed * Mathf.Cos(Mathf.Deg2Rad * startAngle);
    }

    /// <summary>
    /// Returns projection of speed vector on Y axis
    /// The following formula is used for calculation: VY = v * sin(α) * t - g * t, where v - start speed, α - angle between impulse direction and X axis,
    /// g - acceleration of free fall.
    /// </summary>
    /// <param name="startSpeed">Start speed.</param>
    /// <param name="startAngle">Start angle between impulse direction and X axis.</param>
    /// <param name="time">Current flying time.</param>
    /// <returns>Projection value.</returns>
    public static float SpeedYProjection(float startSpeed, float startAngle, float time)
    {
        return startSpeed * Mathf.Sin(Mathf.Deg2Rad * startAngle) - FREE_FALL_ACCELERATION * time;
    }

    /// Returns maximum Y coordinate.
    /// The following formula is used for calculation: y = (v^2 * (sin(α))^2) / (2 * g), where v - start speed, a - angle between X axis and start direction,
    /// g - acceleration of free fall.
    /// </summary>
    /// <param name="startSpeed">Start speed.</param>
    /// <param name="startAngle">Start angle between impulse direction and X axis.</param>
    /// <param name="time">Current flying time.</param>
    /// <returns>Coordiante.</returns>
    public static float MaxHeight(float startSpeed, float startAngle)
    {
        return (startSpeed * startSpeed * Mathf.Pow(Mathf.Sin(Mathf.Deg2Rad * startAngle), 2)) / (2 * FREE_FALL_ACCELERATION);
    }

    /// Returns speed in specified time.
    /// </summary>
    /// <param name="startSpeed">Start speed.</param>
    /// <param name="startAngle">Start angle between impulse direction and X axis.</param>
    /// <param name="time">Current flying time.</param>
    /// <returns>Speed.</returns>
    internal static float CurrentSpeed(float startSpeed, float startAngle, float flyingTime)
    {
        float vx = SpeedXProjection(startSpeed, startAngle);
        float vy = SpeedYProjection(startSpeed, startAngle, flyingTime);
        
        return Mathf.Sqrt(vx * vx + vy * vy);
    }
}