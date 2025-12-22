using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;      // ¸úËæµÄÄ¿±ê£¨PlayerPlane£©
    public float followSpeed = 5f;
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position + offset;
        transform.position = Vector3.Lerp(
            transform.position,
            targetPos,
            followSpeed * Time.deltaTime
        );
    }
}
