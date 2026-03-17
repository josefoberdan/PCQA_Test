using UnityEngine;

public class BillboardPointCloud : MonoBehaviour
{
    public Transform cameraTarget;
    public float distance = 2f;
    public float heightOffset = -0.3f;
    public float scaleFactor = 0.01f;

    void LateUpdate()
    {
        if (!cameraTarget) return;
        Vector3 pos = cameraTarget.position +
                      cameraTarget.forward * distance +
                      cameraTarget.up * heightOffset;

        transform.position = pos;
        transform.rotation = Quaternion.LookRotation(
            transform.position - cameraTarget.position
        );
        transform.localScale = Vector3.one * scaleFactor;
    }
}

