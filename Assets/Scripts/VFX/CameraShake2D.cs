using System.Collections;
using UnityEngine;

public class CameraShake2D : MonoBehaviour
{
    public Transform camTransform;
    public float duration = 0.35f;
    public float strength = 0.25f;

    private Vector3 _origPos;
    private Coroutine _co;

    private void Awake()
    {
        if (camTransform == null) camTransform = transform;
        _origPos = camTransform.localPosition;
    }

    public void Shake()
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float x = Random.Range(-1f, 1f) * strength;
            float y = Random.Range(-1f, 1f) * strength;
            camTransform.localPosition = _origPos + new Vector3(x, y, 0f);
            yield return null;
        }

        camTransform.localPosition = _origPos;
        _co = null;
    }
}
