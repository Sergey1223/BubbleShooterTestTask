using UnityEngine;

public class Move : MonoBehaviour
{
    public Vector2 anchorPosition;
    public float angleLimiit;
    public float limit;

    private bool mouseIsDown = false;
    private Vector3 plumbLine = -Vector3.up;
    private Vector3 leftLimit = Vector3.zero;
    private Vector3 rightLimit = Vector3.zero;
    private float upperLimit = 0;
    private float lowerLimit = 0;

    private Vector3 railPosition = Vector3.zero;
    private Quaternion railRotation = Quaternion.identity;

    void OnMouseDown()
    {
        mouseIsDown = true;
    }

    void OnMouseUp()
    {
        mouseIsDown = false;
    }

    // Start is called before the first frame update
    void Start()
    {

        lowerLimit = ((Vector3) anchorPosition - transform.position).magnitude;
        upperLimit = lowerLimit + limit;

        railPosition = transform.parent.position;
        railRotation = transform.parent.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        position.z = 0;

        
        if (mouseIsDown)
        {
            Vector3 newPosition;

            if (trySnap(position, out newPosition))
            {
                this.transform.position = newPosition;
            }
            else
            {
                reset();
            }
            
        }
    }

    private bool trySnap(Vector3 position, out Vector3 resultPosition)
    {
        Vector3 deflection = computeDeflection(position);
        float angle = Vector3.Angle(plumbLine, deflection);

        if (position.y >= anchorPosition.y || deflection.magnitude < lowerLimit)
        {
            resultPosition = Vector3.zero;

            return false;
        }

        if (deflection.magnitude > upperLimit)
        {
            // Limiting deflection magnitude
            deflection *= upperLimit / deflection.magnitude;

            resultPosition = (Vector3) anchorPosition + deflection;
            position = resultPosition;
            deflection = computeDeflection(position);
        }

        // Position exceeded left or right limit
        if (angle > angleLimiit)
        {
            // Distance between anchor and side limit
            float deltaX = deflection.magnitude * Mathf.Sin(angleLimiit);
            
            // Reverse if exceeded left limit
            if (position.x < anchorPosition.x)
            {
                deltaX *= -1;
            }

            float abscissa = anchorPosition.x + deltaX;
            float ordinate = anchorPosition.y - deflection.magnitude * Mathf.Cos(angleLimiit);

            resultPosition = new Vector3(abscissa, ordinate);
        }
        else
        {
            resultPosition =  position;
        }

        return true;
    }

    private Vector3 computeDeflection(Vector3 position)
    {
        return position - (Vector3) anchorPosition;
    }

    void reset ()
    {
        transform.parent.position = railPosition;
        transform.parent.rotation = railRotation;
    }
}
