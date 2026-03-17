using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameFlowManager : MonoBehaviour
{
    [Header("Canvas")]
    public GameObject canvasTutorial;
    public GameObject canvasVotacao;
    public GameObject canvasResultado;

    [Header("Timers UI")]
    public Text textoTimerVisualizacao;   
    public Text textoTimerVotacao;        

    [Header("Times")]
    public int tempoVisualizacao = 15; 
    public int tempoVotacao = 10;      

    private bool votacaoLiberada = false;
    public PointCloudSequencePlayer cloudPlayer;
    
    public SceneFlowController sceneFlow;
    


    void Start()
    {
        //canvasTutorial.SetActive(true);
        //canvasVotacao.SetActive(false);
        //canvasResultado.SetActive(false);
    }

    public void IniciarExperimento()
    {
        canvasTutorial.SetActive(false);
        canvasVotacao.SetActive(true);

        
    }

    IEnumerator FluxoCompleto()
    {
        votacaoLiberada = false;
        textoTimerVotacao.gameObject.SetActive(false); 
        textoTimerVisualizacao.gameObject.SetActive(true);

        int t = tempoVisualizacao;
        while (t > 0)
        {
            textoTimerVisualizacao.text = "Tempo de visualização: " + t.ToString();
            yield return new WaitForSeconds(1f);
            t--;
        }

        textoTimerVisualizacao.gameObject.SetActive(false);

        votacaoLiberada = true;
        textoTimerVotacao.gameObject.SetActive(true);

        int v = tempoVotacao;
        while (v > 0)
        {
            textoTimerVotacao.text = "Tempo restante: " + v.ToString();
            yield return new WaitForSeconds(1f);
            v--;
        }

        FinalizarVotacao();
    }

    public void RegistrarVoto(int nota)
    {
        if (!votacaoLiberada) return;

        Debug.Log("Voto recebido: " + nota);

        // FinalizarVotacao();
    }

    public void FinalizarVotacao()
    {
        votacaoLiberada = false;

        canvasVotacao.SetActive(false);
        canvasResultado.SetActive(true);

        Debug.Log("Votação finalizada.");
    }
    
    public void OnVoltarAoMenu()
{
    StopAllCoroutines();
    votacaoLiberada = false;

    if (textoTimerVisualizacao != null)
        textoTimerVisualizacao.gameObject.SetActive(false);

    if (textoTimerVotacao != null)
        textoTimerVotacao.gameObject.SetActive(false);

    if (cloudPlayer != null)
        cloudPlayer.ResetToFirstFrame();

    if (sceneFlow != null)
        sceneFlow.BackToMenuFromResults();
    else
        Debug.LogError("[GameFlowManager] SceneFlowController não atribuído!");
}



}

