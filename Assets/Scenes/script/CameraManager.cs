using UnityEngine;
using System.Collections;

public class CameraManager : MonoBehaviour
{
    [Header("Camera Sequence")]
    [SerializeField] private float cooldown = 0f;

    private Camera cam;

    void Awake()
    {
        Debug.Log("AwakeSequence jalan di: " + gameObject.name);

        cam = GetComponent<Camera>(); // AUTO AMBIL CAMERA SENDIRI
    
        if (cam == null)
        {
            Debug.LogError("Tidak ada komponen Camera di object ini!");
            return;
        }

        StartCoroutine(Sequence());
    }

    IEnumerator Sequence()
    {
        cam.enabled = false;
        yield return new WaitForSeconds(cooldown);
        cam.enabled = true;
    }
}
