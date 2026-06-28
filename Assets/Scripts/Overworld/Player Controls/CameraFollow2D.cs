using UnityEngine;

public class CameraFollow2D : MonoBehaviour, IPlayerDependency
{
    private Transform target;
    public float smoothSpeed = 5f;

    public void SetPlayer(GameObject playerObj)
    {
        target = playerObj.transform;
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = new Vector3(
            target.position.x,
            target.position.y,
            transform.position.z
        );

        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            smoothSpeed * Time.deltaTime
        );
    }
}
