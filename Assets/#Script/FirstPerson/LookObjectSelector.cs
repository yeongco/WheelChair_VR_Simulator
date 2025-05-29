using UnityEngine;

[RequireComponent(typeof(Camera))]
public class LookObjectSelector : MonoBehaviour
{
    [SerializeField] float maxDistance = 50f;
    [SerializeField] float viewportRadius = 2f;    // 화면 중앙 허용 반경
    [SerializeField] LayerMask interactableLayers;
    public GameObject lookObject;

    Camera cam;

    void Awake() => cam = GetComponent<Camera>();
    void Update() => lookObject = SelectObject();

    GameObject SelectObject()
    {
        var cols = Physics.OverlapSphere(cam.transform.position, maxDistance, interactableLayers);
        GameObject target = null;
        float bestScreenDist = float.MaxValue;
        float bestDistSq = float.MaxValue;

        foreach (var col in cols)
        {
            Vector3 vp = cam.WorldToViewportPoint(col.transform.position);
            if (vp.z <= 0) continue;

            float dx = vp.x - 0.5f, dy = vp.y - 0.5f;
            float screenDist = Mathf.Sqrt(dx * dx + dy * dy);
            if (screenDist > viewportRadius) continue;

            float distSq = (col.transform.position - cam.transform.position).sqrMagnitude;
            if (screenDist < bestScreenDist
                || (Mathf.Approximately(screenDist, bestScreenDist) && distSq < bestDistSq))
            {
                bestScreenDist = screenDist;
                bestDistSq = distSq;
                target = col.gameObject;
            }
        }

        return target;
    }
}
