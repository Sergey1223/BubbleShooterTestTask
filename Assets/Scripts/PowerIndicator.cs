using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PowerIndicator : MonoBehaviour
{
    public Color start;
    public Color end;

    [Tooltip("Maximum distance between poins.")]
    public float range;

    [Tooltip("UI element which will render power as numer.")]
    public GameObject digitalIndicator;

    private Vector2 initialPosition;
    private float colorRange;

    // Start is called before the first frame update
    void Start()
    {
        initialPosition = GetComponent<LineRenderer>().GetPosition(1);

        colorRange = end.r - start.r;
    }

    internal void UpdateValue(float value)
    {
        GetComponent<LineRenderer>().SetPosition(1, new Vector2(initialPosition.x, initialPosition.y + range * value));

        Color newEndColor = new Color(start.r + colorRange * value, start.g + colorRange * value, start.b);
        GetComponent<LineRenderer>().endColor = newEndColor;

        digitalIndicator.GetComponent<Text>().text = (value * 100).ToString();
    }
}
