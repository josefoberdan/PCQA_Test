using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.XR;

public class VRJoystickUISelector : MonoBehaviour
{
    [Header("Botões para Navegar")]
    public Button[] buttons;

    [Header("Navegação")]
    public int currentIndex = 0;
    public float axisThreshold = 0.7f;
    public float repeatDelay = 0.25f;

    [Header("Clique")]
    public float clickCooldown = 0.5f;

    [Header("clique")]
    [Tooltip("só clica se o Ray/pointer estiver em cima do botão (hover real).")]
    public bool requireHoverToClick = true;

    [Header("Feedback Visual")]
    public Color normalColor = Color.white;
    public Color highlightColor = new Color(1f, 0.65f, 0f, 1f);
    public float highlightScale = 1.2f;

    private float lastMoveTime;
    private float lastClickTime;
    private bool triggerWasPressed = false;

    private Vector3[] startScales;

    void Awake()
    {
        if (buttons == null) buttons = new Button[0];

        startScales = new Vector3[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;

            startScales[i] = buttons[i].transform.localScale;

            
         
            if (buttons[i].GetComponent<UIHoverFlag>() == null)
                buttons[i].gameObject.AddComponent<UIHoverFlag>();
        }
    }

    void OnEnable()
    {
       
       
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        if (buttons != null && buttons.Length > 0)
            HighlightButton(Mathf.Clamp(currentIndex, 0, buttons.Length - 1));
    }

    void Update()
    {
    
        InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (!device.isValid) return;

        Vector2 axis;
        device.TryGetFeatureValue(CommonUsages.primary2DAxis, out axis);

        if (Mathf.Abs(axis.x) > axisThreshold && Time.time - lastMoveTime > repeatDelay)
        {
        
            lastMoveTime = Time.time;
            MoveSelection(axis.x > 0 ? +1 : -1);
            
        }
       

        bool triggerPressed;
        device.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);

        
        
        if (!triggerPressed && triggerWasPressed)
            TryClick();

        triggerWasPressed = triggerPressed;
    }

    void TryClick()
    {
        if (Time.time - lastClickTime < clickCooldown) return;
        lastClickTime = Time.time;

        if (buttons == null || buttons.Length == 0) return;

        var bt = buttons[currentIndex];
        if (bt == null || !bt.interactable) return;

        if (requireHoverToClick)
        {
        
            var hover = bt.GetComponent<UIHoverFlag>();
            if (hover == null || !hover.IsHovered)
                return; 
                
        }

        bt.onClick.Invoke();
    }

    void MoveSelection(int dir)
    {
        if (buttons == null || buttons.Length == 0) return;

        currentIndex += dir;
        if (currentIndex < 0) currentIndex = buttons.Length - 1;
        
        
        if (currentIndex >= buttons.Length) currentIndex = 0;

        HighlightButton(currentIndex);
    }

    void HighlightButton(int index)
    {
        if (buttons == null || buttons.Length == 0) return;

        for (int i = 0; i < buttons.Length; i++)
        {
            var b = buttons[i];
            if (b == null) continue;

            var colors = b.colors;
            
            colors.normalColor = normalColor;
            
            colors.highlightedColor = normalColor;
            
            b.colors = colors;

            if (i < startScales.Length)
                b.transform.localScale = startScales[i];
        }

        var bt = buttons[index];
        if (bt == null) return;

        var hc = bt.colors;
        hc.normalColor = highlightColor;
        
        hc.highlightedColor = highlightColor;
        
        bt.colors = hc;

        if (index < startScales.Length)
            bt.transform.localScale = startScales[index] * highlightScale;

   
   
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void HighlightNextButton(Button b)
    {
        int idx = System.Array.IndexOf(buttons, b);
        if (idx >= 0)
        {
        
            currentIndex = idx;
            HighlightButton(currentIndex);
            
        }
    }
}

