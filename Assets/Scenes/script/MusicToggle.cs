using UnityEngine;
using UnityEngine.UI;

public class MusicToggle : MonoBehaviour
{
    public AudioSource musicSource;   // Drag AudioSource music ke sini
    public Button toggleButton;       // Drag Button ke sini
    public Sprite musicOnIcon;        // Icon saat music ON
    public Sprite musicOffIcon;       // Icon saat music OFF

    private bool isMusicOn;

    void Start()
    {
        // Ambil status dari PlayerPrefs (default = ON)
        isMusicOn = PlayerPrefs.GetInt("MusicStatus", 1) == 1;

        ApplyMusicState();
    }

    public void ToggleMusic()
    {
        isMusicOn = !isMusicOn;

        PlayerPrefs.SetInt("MusicStatus", isMusicOn ? 1 : 0);
        PlayerPrefs.Save();

        ApplyMusicState();
    }

    void ApplyMusicState()
    {
        musicSource.mute = !isMusicOn;

        if (toggleButton != null)
        {
            toggleButton.image.sprite = isMusicOn ? musicOnIcon : musicOffIcon;
        }
    }
}
