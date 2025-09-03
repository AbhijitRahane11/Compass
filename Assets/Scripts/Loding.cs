using UnityEngine;
using UnityEngine.UI;

public class Loding : MonoBehaviour
{
    public Image img;
    private void Update()
    {
        img = GetComponent<Image>();
        img.gameObject.transform.Rotate(0, 0, -(120 * Time.deltaTime));
    }
}
