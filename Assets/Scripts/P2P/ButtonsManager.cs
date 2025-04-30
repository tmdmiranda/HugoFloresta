using UnityEngine;

public class ButtonsManager : MonoBehaviour
{
    public GameObject hostPanel;
    public GameObject canvas;
    public void OnClickApareceHost()
    {
        GameObject host = Instantiate(hostPanel);
        host.transform.SetParent(canvas.transform, false);
    }
}
