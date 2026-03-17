using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class SceneFlowController : MonoBehaviour
{
    [Header("Canvas")]
    public GameObject canvasTutorial;
    public GameObject canvasMenu;
    public GameObject canvasVotacao;
    public GameObject canvasResultados;

    [Header("Controle")]
    public VotingController votingController;
    public ExperimentCloudManager experimentManager;

    [Header("Nuvem (OBJETO 3D NA CENA)")]
    public GameObject cloudRoot;

    [Header("Fade (opcional)")]
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 0.4f;

    public GameObject loadingIcon;

    private bool isSwitching = false;

  
    private RuntimePointCloudRenderer cachedRenderer;

    void Start()
    {
        ShowOnly(canvasTutorial);

        
        EnsureCloudLive();
        HideCloudOnly();

        if (fadeCanvasGroup != null)
            fadeCanvasGroup.alpha = 0f;
    }

    private void EnsureCloudLive()
    {
    
        if (cloudRoot != null)
            cloudRoot.SetActive(true);

        if (experimentManager != null && experimentManager.player != null)
        {
            experimentManager.player.enabled = true;

            if (cachedRenderer == null)
                cachedRenderer = experimentManager.player.GetComponent<RuntimePointCloudRenderer>();
        }
    }

    private void HideCloudOnly()
    {
        EnsureCloudLive();

        if (cachedRenderer != null)
            cachedRenderer.Clear();

        
        if (experimentManager != null && experimentManager.player != null && experimentManager.player.loadingUI != null)
            experimentManager.player.loadingUI.SetVisible(false);
    }

    public void GoToMenu()
    {
        StartCoroutine(SwitchScreen(canvasTutorial, canvasMenu));
    }

    public void StartExperiment()
    {
        StartCoroutine(StartExperimentRoutine());
    }

    private IEnumerator StartExperimentRoutine()
    {
        yield return StartCoroutine(SwitchScreen(canvasMenu, canvasVotacao));

        
        HideCloudOnly();

        if (experimentManager != null)
        {
            experimentManager.ResetAndShuffle();
            bool ok = experimentManager.TryLoadNext();
            Debug.Log($"[SceneFlowController] Primeira nuvem carregada? {ok}");
        }
        else
        {
            Debug.LogError("[SceneFlowController] experimentManager NÃO atribuído!");
        }

        if (votingController != null)
            votingController.StartVotingFlow();
    }

    public void GoToResults()
    {
        if (votingController != null)
            votingController.PrepareResultsUI();

        ShowOnly(canvasResultados);

       
        HideCloudOnly();
    }

    public void BackToMenuFromResults()
    {
        StopAllCoroutines();
        
        
        StartCoroutine(SwitchScreen(canvasResultados, canvasMenu));

        if (votingController != null)
            votingController.StopAllCoroutines();

        if (experimentManager != null)
            experimentManager.ResetAndShuffle();

        HideCloudOnly();
    }

    IEnumerator SwitchScreen(GameObject from, GameObject to)
    {
        if (isSwitching) yield break;
        isSwitching = true;

        if (loadingIcon != null)
            loadingIcon.SetActive(true);

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true;
            yield return Fade(0f, 1f);
        }

        ShowOnly(to);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (fadeCanvasGroup != null)
        {
            yield return Fade(1f, 0f);
            fadeCanvasGroup.blocksRaycasts = false;
        }

        if (loadingIcon != null)
            loadingIcon.SetActive(false);

        isSwitching = false;
    }

    private void ShowOnly(GameObject canvas)
    {
    
        if (canvasTutorial != null) canvasTutorial.SetActive(canvas == canvasTutorial);
        
        if (canvasMenu != null) canvasMenu.SetActive(canvas == canvasMenu);
        
        if (canvasVotacao != null) canvasVotacao.SetActive(canvas == canvasVotacao);
        
        if (canvasResultados != null) canvasResultados.SetActive(canvas == canvasResultados);
        
    }

    IEnumerator Fade(float a, float b)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(a, b, t / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = b;
    }

    public void ExitApp()
    {
        Application.Quit();
    }
}

