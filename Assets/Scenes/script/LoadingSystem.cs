using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingSystem : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private int difficultDelimeter = 5;
    [SerializeField] private bool isFlaskCountOverride;
    [SerializeField] private int flaskCountOverride;
    [SerializeField] private bool isLevelOverride;
    [SerializeField] private int levelOverride;
    [SerializeField] private Animation loadingFadeAnimation;
    [SerializeField] private float preLoadingDelay = 0.5f;
    [SerializeField] private int maxFlaskCount = 15;
    private int calculatedFlaskCount;

    private FlaskInitializer flaskInitializer;

    private void Start()
    {
        BeginLoading();
    }

    private void BeginLoading()
    {
        StartCoroutine(LoadingDelay());
    }

    private void Awake()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(true);
    }

    private IEnumerator LoadingDelay()
    {
        yield return new WaitForSeconds(preLoadingDelay);
        StartGame();
    }

    private void StartGame()
    {
        // Use PlayerPrefs instead of YandexGame
        int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);

        if (isLevelOverride)
        {
            currentLevel = levelOverride;
            PlayerPrefs.SetInt("CurrentLevel", currentLevel);
            PlayerPrefs.Save();
        }

        flaskInitializer = GetComponent<FlaskInitializer>();
        
        if (flaskInitializer == null)
        {
            Debug.LogError("FlaskInitializer component is missing on this GameObject!");
            return;
        }

        if (isFlaskCountOverride)
        {
            if (flaskCountOverride > maxFlaskCount)
            {
                flaskInitializer.FlaskCount = maxFlaskCount;
            }
            else
            {
                flaskInitializer.FlaskCount = flaskCountOverride;
            }
        }
        else
        {
            // Level 1-4 logic
            if (currentLevel >= 1 && currentLevel <= 4)
            {
                calculatedFlaskCount = 5;
                // CameraInitializer logic removed because the script is missing in this project
                // if (_camera.aspect < 1) GetComponent<CameraInitializer>().Margin = 1f;
            }
            else
            {
                calculatedFlaskCount = 5 + currentLevel / difficultDelimeter;
            }

            if (calculatedFlaskCount > maxFlaskCount)
            {
                flaskInitializer.FlaskCount = maxFlaskCount;
            }
            else
            {
                flaskInitializer.FlaskCount = calculatedFlaskCount;
            }
        }

        // Adjust rows based on aspect ratio
        if (_camera.aspect > 1)
            flaskInitializer.FlaskRowCount = 6;

        // Note: CameraInitializer modifications for margin are removed because the script is missing.
        // You can implement your own camera adjustment logic here if needed.

        flaskInitializer.InitializeFlasks();
        
        // Directly disable loading panel after initialization since we don't have GlobalEvents
        DisableLoadingPanel();
    }

    private void DisableLoadingPanel()
    {
        if (loadingFadeAnimation != null)
            loadingFadeAnimation.Play();
        else if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }
}

