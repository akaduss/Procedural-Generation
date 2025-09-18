using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class GraphicNode : MonoBehaviour
{

    public int index;
    public bool isControl;

    [HideInInspector]
    public bool active;

    public TextMeshProUGUI text;

    Color controlSel;
    Color controlDes;

    Color sel;
    Color des;

    void Awake()
    {
        controlSel = Col(161, 62, 62);
        controlDes = Col(255, 255, 255);

        sel = Col(225, 225, 225);
        des = Col(105, 105, 105);
        text.text = "";
        Image i = GetComponent<Image>();
        if (!isControl)
        {
            i.color = (active) ? sel : des;
        }
        else
        {
            i.color = (active) ? controlSel : controlDes;
        }
    }

    Color Col(float r, float g, float b)
    {
        return new Color(r / 255f, g / 255f, b / 255f);
    }

    public void Toggle()
    {
        active = !active;
        Image i = GetComponent<Image>();

        if (!isControl)
        {
            i.color = (active) ? sel : des;
        }
        else
        {
            i.color = (active) ? controlSel : controlDes;
        }

        if (!active)
            text.text = "";
    }

    public void SetActive()
    {
        active = false;
        Toggle();
    }

    public void Clear()
    {
        active = true;
        Toggle();
    }

}