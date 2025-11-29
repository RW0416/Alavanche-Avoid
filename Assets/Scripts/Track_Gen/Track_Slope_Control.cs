using UnityEngine;

public class TrackSlope : MonoBehaviour
{
    [Range(0f, 45f)]
    public float slopeDegrees = 15f;


    public bool downhillAlongNegativeZ = true;

    void Awake()
    {
        float angle = downhillAlongNegativeZ ? -slopeDegrees : slopeDegrees;
        transform.rotation = Quaternion.Euler(angle, 0f, 0f);
    }
}
