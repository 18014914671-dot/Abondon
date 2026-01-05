using UnityEngine;

public class PlayAnimatorOnEnable : MonoBehaviour
{
    public string stateName = "Burst";
    private Animator _anim;

    private void Awake() => _anim = GetComponent<Animator>();

    private void OnEnable()
    {
        if (!_anim) return;
        _anim.Rebind();
        _anim.Update(0f);
        _anim.Play(stateName, 0, 0f);
    }
}
