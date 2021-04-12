using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Claim : MonoBehaviour
{
    [Tooltip("Capacity.")]
    public int capacity;

    [Tooltip("Distance between bubbles centers.")]
    public float interval;

    [Tooltip("Offset to up regarding claim center.")]
    public float offset;

    private List<GameObject> container;

    // Start is called before the first frame update
    void Start()
    {
        container = new List<GameObject>(capacity);
    }

    public void Push(GameObject bubble)
    {
        bubble.transform.parent = transform;
        bubble.transform.localPosition = new Vector3(-container.Count * interval, offset, 0);
        bubble.GetComponent<CircleCollider2D>().enabled = false;

        container.Insert(0, bubble);    
    }

    public GameObject Pop()
    {
        if (container.Count == 0)
        {
            return null;
        }

        GameObject result = container[container.Count - 1];
        result.transform.parent = null;
        result.GetComponent<CircleCollider2D>().enabled = true;

        container.Remove(result);

        foreach (GameObject bubble in container)
        {
            bubble.transform.localPosition = new Vector3(bubble.transform.localPosition.x + interval, offset, 0);
        }

        return result;
    }
}
