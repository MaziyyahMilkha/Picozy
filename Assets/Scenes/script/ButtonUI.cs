using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonUI : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject backgroundMain;
    public GameObject backgroundTheme;

    [Header("Puzzle Panels")]
    public GameObject pausePanel; // Masukkan "pausebg" ke sini di scene Puzzle

    [Header("Other Objects")]
    public GameObject pos, detector;

    void Start()
    {
        // Jika di scene Menu, pastikan Main Menu muncul
        if (backgroundMain != null) ShowMainMenu();
    }

    void Update()
    {
        // Deteksi tombol Back di HP Android (KeyCode.Escape)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleBackButton();
        }
    }

    private void HandleBackButton()
    {
        // 1. Jika di Level (Pause Menu ada dan sedang aktif) -> Tutup Pause
        if (pausePanel != null && pausePanel.activeSelf)
        {
            pausePanel.SetActive(false);
            return;
        }

        // 2. Jika di Level (Pause Menu ada tapi sedang TIDAK aktif) -> Buka Pause
        if (pausePanel != null && !pausePanel.activeSelf)
        {
            pausePanel.SetActive(true);
            return;
        }

        // 3. Jika di Menu (Sedang di Select Theme) -> Kembali ke Main Menu
        if (backgroundTheme != null && backgroundTheme.activeSelf)
        {
            ShowMainMenu();
            return;
        }

        // 4. Jika di Menu (Sedang di Main Menu) -> Exit Game
        if (backgroundMain != null && backgroundMain.activeSelf)
        {
            OneExitClick();
        }
    }
    
    // Fungsi dipanggil saat tombol PLAY ditekan
    public void OpenThemeSelection()
    {
        if (backgroundMain != null) backgroundMain.SetActive(false);
        if (backgroundTheme != null) backgroundTheme.SetActive(true);
    }

    // Fungsi dipanggil saat tombol CLOSE ditekan (di menu tema)
    public void ShowMainMenu()
    {
        if (backgroundMain != null) backgroundMain.SetActive(true);
        if (backgroundTheme != null) backgroundTheme.SetActive(false);
    }

    public void LoadToScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void OneExitClick()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
