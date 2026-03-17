using UnityEngine;

using UnityEngine.UI;

public class CloudNameLabel : MonoBehaviour
{
    [Header("UI")]
    public Text nameText;
    
    public CanvasGroup canvasGroup;

    private void Awake()
    {
        Hide();
    }

    public void Show(string cloudName)
    {
        if (nameText != null)
            nameText.text = cloudName;

        if (canvasGroup != null)
        {
        
            canvasGroup.alpha = 1f;
            
            canvasGroup.interactable = false;
            
            canvasGroup.blocksRaycasts = false;
            
        }

        Debug.Log($"[CloudNameLabel] Show: {cloudName}");
    }

    public void Hide()
    {
        if (canvasGroup != null)
        {
        
            canvasGroup.alpha = 0f;
            
            canvasGroup.interactable = false;
            
            canvasGroup.blocksRaycasts = false;
            
        }

        Debug.Log("[CloudNameLabel] Hide");
    }
}

