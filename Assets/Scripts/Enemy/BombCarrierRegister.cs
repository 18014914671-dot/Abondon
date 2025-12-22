using UnityEngine;

public class BombCarrierRegister : MonoBehaviour
{
    private BombCarrierEnemy bomb;

    private void Awake()
    {
        bomb = GetComponent<BombCarrierEnemy>();
    }

    private void OnEnable()
    {
        if (bomb != null && WordBombManager.Instance != null)
        {
            WordBombManager.Instance.Register(bomb);
        }
    }

    private void OnDisable()
    {
        if (bomb != null && WordBombManager.Instance != null)
        {
            WordBombManager.Instance.Unregister(bomb);
        }
    }
}
