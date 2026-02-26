using UnityEngine;

public class UIChanger : MonoBehaviour
{
    public GameObject uiSekarang;
    public GameObject uiTujuan;

    public void GantiPanel()
    {
        uiSekarang.SetActive(false);
        uiTujuan.SetActive(true);
    }
}
