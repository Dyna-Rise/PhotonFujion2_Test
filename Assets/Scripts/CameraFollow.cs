using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Vector3 offset = new Vector3(0, 5, -7);
    [SerializeField] float smooth = 10f;

    Transform target;

    public void SetTarget(Transform t)
    {
        target = t;
        transform.position = target.position + offset;
        transform.LookAt(target);
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smooth * Time.deltaTime);
        transform.LookAt(target);
    }
}
