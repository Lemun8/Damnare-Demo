using UnityEngine;
using System.Collections;

public class BattleCameraController : MonoBehaviour
{
    public static BattleCameraController Instance;

    [Header("Camera")]
    public Camera battleCamera;

    [Header("Positions")]
    public Transform battlefieldView;   // wide view
    public float panSpeed = 3f;

    Transform currentTarget;
    Coroutine panRoutine;

    void Awake()
    {
        Instance = this;
        if (battleCamera == null)
            battleCamera = Camera.main;
    }

    public void FocusOn(Transform target)
    {
        if (target == null) return;

        currentTarget = target;
        StartPan();
    }

    public void FocusBattlefield()
    {
        if (battlefieldView == null) return;

        currentTarget = battlefieldView;
        StartPan();
    }

    void StartPan()
    {
        if (panRoutine != null)
            StopCoroutine(panRoutine);

        panRoutine = StartCoroutine(PanToTarget());
    }

    IEnumerator PanToTarget()
    {
        while (Vector3.Distance(battleCamera.transform.position, currentTarget.position) > 0.05f)
        {
            battleCamera.transform.position =
                Vector3.Lerp(
                    battleCamera.transform.position,
                    currentTarget.position,
                    Time.deltaTime * panSpeed
                );

            battleCamera.transform.rotation =
                Quaternion.Lerp(
                    battleCamera.transform.rotation,
                    currentTarget.rotation,
                    Time.deltaTime * panSpeed
                );

            yield return null;
        }
    }
}
