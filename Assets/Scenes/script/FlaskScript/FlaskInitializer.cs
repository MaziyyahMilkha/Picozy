using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.AI;
// using Unity.AI.Navigation; // Add this if you have the AI Navigation package installed
// using YG; // Removed missing dependency

public class FlaskInitializer : MonoBehaviour
{
    public static int emptyFlaskCount = 2;
    private int flaskCount;
    [SerializeField] private int flaskRowCount;

    [Header("Prefabs")]
    [SerializeField] private GameObject groundPrefab;
    [SerializeField] private GameObject flaskPrefab;

    [Header("Flask offset")]
    [SerializeField] private float offsetX = 5.75f;
    [SerializeField] private float offsetY = 0.25f;
    [SerializeField] private float offsetZ = -15f;

    [SerializeField] private GameObject[] spawnedFlasks;

    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private GameObject addNewFlaskBtn;

    [Space(15)]
    [SerializeField] private GameObject canvasChanger;

    private List<Vector3> calculatedPositions = new List<Vector3>();

    private GameObject spawnedGround;

    private int filledFlask;

    public int FilledFlask { get => filledFlask; }
    public int FlaskCount { get => flaskCount; set => flaskCount = value; }
    public int FlaskRowCount { get => flaskRowCount; set => flaskRowCount = value; }
    public GameObject[] SpawnedFlasks { get => spawnedFlasks;}

    private void OnEnable()
    {
        // YandexGame.RewardVideoEvent += AddNewFlaskRewarded; // Removed
    }

    private void OnDisable()
    {
        // YandexGame.RewardVideoEvent -= AddNewFlaskRewarded; // Removed
    }

    public void InitializeFlasks(bool isNeedToAddNewFlask = false)
    {
        // Replaced YandexGame.savesData.currentLevel with PlayerPrefs
        int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        
        if (currentLevel >= 35)
            emptyFlaskCount = 3;
            
        filledFlask = flaskCount - emptyFlaskCount;
        if (isNeedToAddNewFlask)
        {
            flaskCount++;
        }

        spawnedFlasks = new GameObject[flaskCount];

        if (!isNeedToAddNewFlask && groundPrefab != null)
            spawnedGround = Instantiate(groundPrefab, Vector3.zero, Quaternion.identity);

        if (isNeedToAddNewFlask)
            calculatedPositions.Clear();

        Vector3 spawnPosition = Vector3.zero;

        for (int i = 0; i < flaskCount; i++)
        {
            int row = i / flaskRowCount;
            int column = i % flaskRowCount;

            // Simple grid layout logic
            if (row < flaskCount / flaskRowCount)
            {
                spawnPosition = new Vector3(offsetX * column, offsetY, row * offsetZ);
            }

            if (flaskCount % flaskRowCount > 0 && row == flaskCount / flaskRowCount)
            {
                float newOffsetX = offsetX * (flaskRowCount - 1) / ((flaskCount % flaskRowCount) + 1);

                spawnPosition = new Vector3(newOffsetX + newOffsetX * column, offsetY, row * offsetZ);

                if (flaskCount % flaskRowCount == flaskRowCount - 1)
                {
                    newOffsetX = offsetX * (flaskRowCount - 2) / ((flaskCount % flaskRowCount) - 1);
                    spawnPosition = new Vector3(newOffsetX / 2 + newOffsetX * column, offsetY, row * offsetZ);
                }

            }
            calculatedPositions.Add(spawnPosition);
        }
        
        InstantiateFlask(isNeedToAddNewFlask);
        
        // GlobalEvents.SendFlaskInitialized(); // Removed missing dependency
        
        if (canvasChanger != null)
            canvasChanger.SetActive(true);
            
        StartInitializingCamera(spawnedFlasks);
        
        if (spawnedGround != null)
        {
            BuildNavMeshPath(spawnedGround);
            EnableNavMeshAgentsOnBots();
        }

        if (!isNeedToAddNewFlask)
            StartInitializingBots(spawnedFlasks);
    }

    private void EnableNavMeshAgentsOnBots()
    {
        if (spawnedGround == null) return;
        
        var navAgents = spawnedGround.GetComponentsInChildren<NavMeshAgent>();
        foreach (var item in navAgents)
        {
            item.enabled = true;
        }
    }

    private void InstantiateFlask(bool isNeedToAddNewFlask)
    {
        if (isNeedToAddNewFlask)
        {
            // Note: FlaskController is missing, so we just handle the Transform or generic Component if needed.
            /*
            var flaskControllers = spawnedGround.GetComponentsInChildren<FlaskController>();
            for (int i = 0; i < flaskControllers.Length; i++)
            {
                spawnedFlasks[i] = flaskControllers[i].gameObject;
                var navAgents = spawnedFlasks[i].GetComponentsInChildren<NavMeshAgent>();
                foreach (var item in navAgents)
                {
                    item.enabled = false;
                }
                spawnedFlasks[i].transform.position = calculatedPositions[i];
            }
            */
            
            // Fallback: Just move existing flasks if we can track them, or re-instantiate.
            // For now, assuming spawnedFlasks array from previous frame isn't valid, we might need a better way to track them without FlaskController.
            // As a simple fix to compile, we proceed with instantiation logic only for the new one if possible, but without FlaskController logic.

            if (flaskPrefab != null && spawnedGround != null)
            {
                 spawnedFlasks[flaskCount - 1] = Instantiate(flaskPrefab, calculatedPositions[calculatedPositions.Count - 1], flaskPrefab.transform.rotation, spawnedGround.transform);
                 // spawnedFlasks[flaskCount - 1].GetComponent<FlaskController>().InitializeComponent(); // Removed
            }

            // CameraInitializer removed
            /*
            if (Camera.main.aspect < 0.7f && FlaskCount == 16)
                GetComponent<CameraInitializer>().Margin = 0.6f;
            if (Camera.main.aspect >= 0.7f && Camera.main.aspect < 1 && FlaskCount == 16)
                GetComponent<CameraInitializer>().Margin = 0.75f;
            if (Camera.main.aspect < 1 && FlaskCount == 11)
                GetComponent<CameraInitializer>().Margin = 0.7f;
            */
            return;
        }
        
        for (int i = 0; i < calculatedPositions.Count; i++)
        {
            if (flaskPrefab != null && spawnedGround != null)
                spawnedFlasks[i] = Instantiate(flaskPrefab, calculatedPositions[i], flaskPrefab.transform.rotation, spawnedGround.transform);
        }
    }

    public void AddNewFlask()
    {
        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
            var canvasGroup = loadingPanel.GetComponent<CanvasGroup>();
            if (canvasGroup != null) canvasGroup.alpha = 1f;
        }

        // Mocking Reward Logic directly
        Debug.Log("Playing Rewarded Video (Mock)...");
        AddNewFlaskRewarded(2);
    }

    private void AddNewFlaskRewarded(int id)
    {
        if (id == 2)
        {
            InitializeFlasks(true);
            if (addNewFlaskBtn != null)
                addNewFlaskBtn.GetComponent<Button>().interactable = false;
            
            // GlobalEvents.SendNewFlaskAdded(); // Removed
        }

    }
    private void StartInitializingBots(GameObject[] flasks)
    {
        // BotInitializer removed because script is missing
        /*
        BotInitializer initializer = GetComponent<BotInitializer>();
        if (initializer != null) initializer.Initialize(flasks);
        */
    }
    private void StartInitializingCamera(GameObject[] flasks)
    {
        // CameraInitializer removed because script is missing
        /*
        CameraInitializer cameraInitializer = GetComponent<CameraInitializer>();
        if (cameraInitializer != null) cameraInitializer.InitializeCameraPositionAndRotation(flasks);
        */
    }

    private void BuildNavMeshPath(GameObject surface)
    {
        // Requires Unity.AI.Navigation package for NavMeshSurface
        // If you don't have it, this will error. Commenting out to be safe.
        /*
        var navMeshSurf = surface.GetComponent<NavMeshSurface>();
        if (navMeshSurf != null)
        {
            navMeshSurf.RemoveData();
            navMeshSurf.BuildNavMesh();
        }
        */
    }
}
