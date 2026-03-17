using UnityEngine;
using UnityEngine.UI;

public class LoadingMessageBillboard : MonoBehaviour
{
    [Header("Referências")]
    public Transform cameraTarget;
    public CanvasGroup canvasGroup;
    public Text messageText;

    [Header("Posicionamento (na frente do usuário)")]
    public float distance = 2.0f;
    public float heightOffset = 0.0f;

    [Header("Texto")]
    public string loadingMessage = "Carregando nuvem de pontos...";

    void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        SetVisible(false);
    }

    void LateUpdate()
    {
        if (!cameraTarget) return;

        Vector3 forward = cameraTarget.forward;
        forward.y = 0f;
        forward.Normalize();

        transform.position = cameraTarget.position + forward * distance + Vector3.up * heightOffset;

        Vector3 lookDir = transform.position - cameraTarget.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(lookDir);
    }

    public void SetVisible(bool visible)
    {
        if (messageText != null)
            messageText.text = loadingMessage;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            gameObject.SetActive(visible);
        }
    }
}

