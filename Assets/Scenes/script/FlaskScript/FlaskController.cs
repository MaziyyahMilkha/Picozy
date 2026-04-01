using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class FlaskController : MonoBehaviour
{
    [SerializeField] private Transform[] flaskPositions = new Transform[4];
    [SerializeField] private int nextEmptyPositionIndex = -1;
    [SerializeField] private ParticleSystem flaskParticles;

    private Stack<Color> colors = new Stack<Color>();
    private Stack<GameObject> bots = new Stack<GameObject>();

    private GameObject flaskPlane;

    private bool isFilledByOneColor = false;

    private bool isLevelEnd;

    #region Properties
    public GameObject FlaskPlane { get => flaskPlane; }
    public Stack<Color> Colors { get => colors; }
    public Stack<GameObject> Bots { get => bots; }
    public Transform[] FlaskPositions { get => flaskPositions; }
    public int NextEmptyPositionIndex { get => nextEmptyPositionIndex; }
    public bool IsFilledByOneColor { get => isFilledByOneColor; set => isFilledByOneColor = value; }
    #endregion

    private void Awake()
    {
        GlobalEvents.OnBotsInitialized.AddListener(InitializeComponent);
        GlobalEvents.OnLevelEnd.AddListener(TriggerLevelEnd);
        GlobalEvents.OnLevelEnd.AddListener(PlayFlaskParticle);

    }

    private void TriggerLevelEnd(int arg0)
    {
        isLevelEnd = true;
    }

    private void OnDisable()
    {
        GlobalEvents.OnLevelEnd.RemoveListener(PlayFlaskParticle);
        GlobalEvents.OnBotsInitialized.RemoveListener(InitializeComponent);
        GlobalEvents.OnLevelEnd.RemoveListener(TriggerLevelEnd);
    }

    public void InitializeComponent(Bot[] bots = null, bool restart = false)
    {
        InitializeStackColor();
        InitializeFlaskPositions();
        InitializeBotPositions();
        isFilledByOneColor = false;
        GetComponent<MeshRenderer>().material.color = new Color(0.7830188f, 0.7830188f, 0.7830188f);
    }

    private void InitializeBotPositions()
    {
        if (bots.Count != 0)
            bots.Clear();

        for (int i = 0; i < gameObject.transform.childCount - 1; i++)
        {
            if (gameObject.transform.GetChild(i).childCount == 0)
                continue;
            GameObject child = gameObject.transform.GetChild(i).GetChild(0).gameObject;
            //child.GetComponent<NavMeshAgent>().updatePosition = false;
            bots.Push(child);
        }
    }

    private void InitializeFlaskPositions()
    {
        //var positions = transform.GetComponentsInChildren<Transform>().Where(x => x.gameObject.CompareTag("Position")).ToArray();

        if (gameObject.transform.GetChild(0).childCount == 0)
            nextEmptyPositionIndex = 0;
        else
            nextEmptyPositionIndex = 4;

        //for (int i = 1; i < positions.Length; i++)
        //{
        //    flaskPositions[i - 1] = positions[i];
        //}

    }

    private void InitializeStackColor()
    {
        var sunnies = transform.GetComponentsInChildren<Sunny>();
        foreach (var sunny in sunnies)
        {
             var renderer = sunny.GetComponentInChildren<SkinnedMeshRenderer>();
             if (renderer != null)
                 colors.Push(renderer.material.color);
        }
        flaskPlane = transform.Find("Plane").gameObject;
    }

    private bool CheckStackColorFill()
    {
        Color firstColor;
        IEnumerator<Color> enumerator = colors.GetEnumerator();
        enumerator.MoveNext();
        firstColor = enumerator.Current;

        int sameColors = 1;
        while (enumerator.MoveNext())
        {
            if (firstColor != enumerator.Current)
                return false;
            sameColors++;
        }
        return sameColors == 4 ? true : false;
    }

    /// <summary>
    /// ���� ����� ���������� ���� �� ��������� ������� ������
    /// </summary>
    /// <param name="bot">������������ ���</param>
    /// <returns>���������� true, ���� ���� ��������� ����� ��� ���������� ����</returns>
    public bool ProcessBotPosition(GameObject bot)
    {
        colors.Push(bot.GetComponentInChildren<SkinnedMeshRenderer>().material.color);
        bots.Push(bot);


        Transform position = flaskPositions[nextEmptyPositionIndex];
        ShiftNextPositionIndex(1);

        bot.transform.SetParent(position.transform);

        Sunny sunny = bot.GetComponent<Sunny>();
        if (sunny != null)
        {
            Debug.Log($"FlaskController: Moving Sunny to {position.position}");
            sunny.MoveTo(position.position);
        }
        else
        {
            Debug.LogError("FlaskController: Bot missing Sunny component! Adding it dynamically.");
            sunny = bot.AddComponent<Sunny>();
            sunny.MoveTo(position.position);
        }
        
        CheckFilledFlask();

        return Bots.Count == 4 ? false : true;
    }

    private bool CheckFilledFlask()
    {
        isFilledByOneColor = CheckStackColorFill();
        if (isFilledByOneColor)
        {
            GetComponent<MeshRenderer>().material.color = Colors.Peek();
            GlobalEvents.SendFlaskFilledByOneColor();
            PlayFlaskParticle();
            return true;
        }
        return false;
    }

    private void PlayFlaskParticle(int arg0 = 0)
    {
        if (isLevelEnd)
        {
            var main = flaskParticles.main;
            main.loop = true;
        }

        flaskParticles.gameObject.SetActive(true);
        flaskParticles.Play();
    }

    /// <summary>
    /// ���� ����� �������� ��������� �� ������� ��������� 
    /// ������� ������.
    /// </summary>
    /// <param name="mode">0 - ������ �����. 1 - ���������� �����</param>
    public void ShiftNextPositionIndex(int mode)
    {
        if (mode == 0)
        {
            nextEmptyPositionIndex--;
            if (nextEmptyPositionIndex == -1)
                nextEmptyPositionIndex = 3;
        }
        else if (mode == 1)
        {
            nextEmptyPositionIndex++;
            if (nextEmptyPositionIndex == 4)
                nextEmptyPositionIndex = 0;
        }
    }

    private void OnMouseDown()
    {
        GameManager.Instance.SelectFlask(this);
    }

    public bool CanAddSunny(Sunny sunny)
    {
        if (bots.Count >= 4) return false;
        
        // If empty, any color is fine
        if (bots.Count == 0) return true;

        // Verify color match
        if (colors.Count > 0)
        {
            Color topColor = colors.Peek();
            // Assuming Sunny has a way to provide its Color equivalent or we map SunnyType to Color
            // For now, let's assume we can get the color from the sunny object's renderer
            var sunnyRenderer = sunny.GetComponentInChildren<SkinnedMeshRenderer>();
            if (sunnyRenderer != null)
            {
                // Simple color equality check. 
                // Note: Floating point color comparison can be tricky, but Unity's Color == operator handles it reasonably well.
                return topColor == sunnyRenderer.sharedMaterial.color;
            }
        }
        return true;
    }

    public void AddSunny(Sunny sunny)
    {
        ProcessBotPosition(sunny.gameObject);
        sunny.SetFlask(this);
    }

    public void RemoveTopSunny()
    {
        if (bots.Count > 0)
        {
            bots.Pop();
            colors.Pop();
            ShiftNextPositionIndex(0); // Shift back
        }
    }
}
