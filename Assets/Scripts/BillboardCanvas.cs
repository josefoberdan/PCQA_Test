using UnityEngine;

public class BillboardCanvas : MonoBehaviour
{
    public Transform target;
    public float distance = 2f;

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 pos = target.position + target.forward * distance;
        transform.position = pos;
        transform.rotation = Quaternion.LookRotation(transform.position - target.position);
    }
}

