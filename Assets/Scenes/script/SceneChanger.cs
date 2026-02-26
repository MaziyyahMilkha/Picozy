using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public string namaScene;

    public void PindahScene()
    {
        SceneManager.LoadScene(namaScene);
    }
}
