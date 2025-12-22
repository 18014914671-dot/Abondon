using UnityEngine;

public class AutoDestroyAfterAnim : MonoBehaviour
{
    public float lifeTime = 1f;
    private void Start() => Destroy(gameObject, lifeTime);
}
