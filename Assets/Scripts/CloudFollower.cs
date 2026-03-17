using UnityEngine;

public class CloudFollower : MonoBehaviour
{
    public Transform cameraTransform;
    public float distance = 2f;
    public float heightOffset = 0.0f;

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 forward = cameraTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        transform.position = cameraTransform.position + forward * distance + Vector3.up * heightOffset;

       
    }
}

