using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance;

    private class Notification
    {
        public string message;
        public float timeLeft;
        public float fadeDuration = 1f;

        public Notification(string msg, float duration)
        {
            message = msg;
            timeLeft = duration;
        }
    }

    private List<Notification> notifications = new List<Notification>();

    [Header("Overworld Layout")]
    public Vector2 overworldStartPosition = new Vector2(20, 20);
    public float overworldSpacing = 30f;

    [Header("Battle Layout")]
    public Vector2 battleStartPosition = new Vector2(20, 300);
    public float battleSpacing = 25f;

    [Header("General Settings")]
    public float notificationDuration = 3f;

    private Vector2 startPosition;
    private float spacing;

    private GUIStyle notificationStyle;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        notificationStyle = new GUIStyle
        {
            fontSize = 18,
            wordWrap = true,
            normal = { textColor = Color.white }
        };

        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyLayout(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyLayout(scene.name);
    }

    void ApplyLayout(string sceneName)
    {
        if (sceneName.Contains("BattleScene"))
        {
            // 🔥 Battle: top-center
            startPosition = new Vector2(
                Screen.width / 2f, // center horizontally (label width = 500)
                20f                        // top padding
            );
            spacing = battleSpacing;
        }
        else
        {
            // 🌍 Overworld: bottom-left
            startPosition = new Vector2(
                20f,
                Screen.height - 40f        // start near bottom
            );
            spacing = overworldSpacing;
        }
    }

    void Update()
    {
        for (int i = notifications.Count - 1; i >= 0; i--)
        {
            notifications[i].timeLeft -= Time.deltaTime;

            if (notifications[i].timeLeft <= 0)
                notifications.RemoveAt(i);
        }
    }

    void OnGUI()
    {
        foreach (var notif in notifications)
        {
            float alpha = notif.timeLeft < notif.fadeDuration
                ? notif.timeLeft / notif.fadeDuration
                : 1f;

            Color oldColor = GUI.color;
            GUI.color = new Color(1, 1, 1, alpha);

            GUI.Label(
                new Rect(startPosition.x, startPosition.y, 500, 30),
                notif.message,
                notificationStyle
            );

            GUI.color = oldColor;
            // No yOffset increment — notifications overlap at the same position
        }
    }

    public static void Show(string message)
    {
        if (Instance == null)
        {
            Debug.LogWarning("NotificationManager missing!");
            return;
        }

        Instance.notifications.Add(
            new Notification(message, Instance.notificationDuration)
        );
    }
}
