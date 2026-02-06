using UnityEngine;
using System.Collections;

public class AwakeSequence : MonoBehaviour
{
    public GameObject target;   // object yang mau diatur
    public float cooldown = 2f; // waktu tunggu (detik)

    void Awake()
    {
        StartCoroutine(Sequence());
    }

    IEnumerator Sequence()
    {
        // GET (pastikan ada)
        if (target == null)
            target = gameObject;

        // MATIKAN
        target.SetActive(false);

        // TUNGGU
        yield return new WaitForSeconds(cooldown);

        // NYALAKAN LAGI
        target.SetActive(true);
    }
}
