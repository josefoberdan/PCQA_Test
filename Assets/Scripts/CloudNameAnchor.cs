using UnityEngine;

public class CloudNameAnchor : MonoBehaviour
{
    [Header("Objeto raiz da nuvem")]
    public Transform cloudRoot;

    [Header("Câmera (para billboard)")]
    public Transform cameraTransform;

    [Header("Offset acima da nuvem")]
    public Vector3 offset = new Vector3(0f, 1.5f, 0f);

    [Header("Billboard")]
    public bool faceCamera = true;
    public bool lockYRotation = true;

    void LateUpdate()
    {
        if (cloudRoot == null) return;

        transform.position = cloudRoot.position + offset;

        if (faceCamera && cameraTransform != null)
        {
            Vector3 dir = transform.position - cameraTransform.position;

            if (lockYRotation)
                dir.y = 0f;

            transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}

