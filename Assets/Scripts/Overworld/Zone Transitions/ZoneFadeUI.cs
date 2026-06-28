using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ZoneFadeUI : MonoBehaviour
{
    public static ZoneFadeUI Instance;

    private Image fadeImage;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        fadeImage = GetComponentInChildren<Image>();
    }

    public IEnumerator FadeOut(float duration = 0.4f)
    {
        Color c = fadeImage.color;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            c.a = t / duration;
            fadeImage.color = c;
            yield return null;
        }
        c.a = 1;
        fadeImage.color = c;
    }

    public IEnumerator FadeIn(float duration = 0.4f)
    {
        Color c = fadeImage.color;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            c.a = 1f - (t / duration);
            fadeImage.color = c;
            yield return null;
        }
        c.a = 0;
        fadeImage.color = c;
    }
}
