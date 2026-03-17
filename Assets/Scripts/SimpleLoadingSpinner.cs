using UnityEngine;

public class SimpleLoadingSpinner : MonoBehaviour
{
    public float speed = 120f;

    void Update()
    {
        transform.Rotate(0, 0, -speed * Time.deltaTime);
    }
}

