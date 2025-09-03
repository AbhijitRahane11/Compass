using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageBox : MonoBehaviour
{
    public GameObject MessageBoxImg;
    public TMP_Text text;
    public static MessageBox messageBox;

    private void Awake()
    {
        messageBox = this;    
    }

    public void Show(string msg,int delayTime=2)
    {
        _ = ShowMsg(msg, delayTime);
    }
    public async Task ShowMsg(string msg,int delaytime)
    {
        MessageBoxImg.gameObject.SetActive(true);
        text.text = msg;
        await Task.Delay(delaytime*1000);
        MessageBoxImg.gameObject.SetActive( false);
        text.text = "";
    }
}
