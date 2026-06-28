using UnityEngine;
using System.Collections.Generic;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance;

    class Popup
    {
        public Vector3 worldPos;
        public string text;
        public Color color;
        public float time;
        public float yOffset;
    }

    List<Popup> popups = new List<Popup>();

    private void Awake()
    {
        Instance = this;
    }

    public void ShowDamage(Vector3 worldPos, int amount)
    {
        AddPopup(worldPos, amount.ToString(), Color.red);
    }

    public void ShowMiss(Vector3 worldPos)
    {
        AddPopup(worldPos, "MISS", Color.gray);
    }

    public void ShowEvade(Vector3 worldPos)
    {
        AddPopup(worldPos, "EVADE", new Color(0.6f, 0.9f, 1f));
    }

    public void ShowStatus(Vector3 worldPos, string statusName)
    {
        AddPopup(worldPos, statusName.ToUpper(), Color.magenta);
    }

    public void ShowHeal(Vector3 worldPos, int amount)
    {
        AddPopup(worldPos, "+" + amount, Color.green);
    }

    void AddPopup(Vector3 pos, string text, Color color)
    {
        popups.Add(new Popup
        {
            worldPos = pos,
            text = text,
            color = color,
            time = 1.2f,
            yOffset = 0f
        });
    }

    private void Update()
    {
        for (int i = popups.Count - 1; i >= 0; i--)
        {
            popups[i].time -= Time.deltaTime;
            popups[i].yOffset += Time.deltaTime * 0.6f;

            if (popups[i].time <= 0)
                popups.RemoveAt(i);
        }
    }

    private void OnGUI()
    {
        if (Camera.main == null) return;

        foreach (var p in popups)
        {
            Vector3 screen = Camera.main.WorldToScreenPoint(
                p.worldPos + Vector3.up * (0.6f + p.yOffset)
            );

            if (screen.z <= 0) continue;

            GUIStyle st = new GUIStyle(GUI.skin.label);
            st.fontSize = 22;
            st.alignment = TextAnchor.MiddleCenter;

            float alpha = Mathf.Clamp01(p.time / 0.4f);
            st.normal.textColor = new Color(p.color.r, p.color.g, p.color.b, alpha);

            Rect r = new Rect(
                screen.x - 50,
                Screen.height - screen.y - 25,
                100,
                50
            );

            GUI.Label(r, p.text, st);
        }
    }
}
