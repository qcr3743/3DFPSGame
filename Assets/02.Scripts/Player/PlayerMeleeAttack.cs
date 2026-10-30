using UnityEngine;

// 근접 모드에서 마우스 오른쪽 버튼을 누르면 카타나를 휘두른다.
// 애니메이션 이벤트를 받아야 하므로 Animator가 붙은 Model에 붙임
public class PlayerMeleeAttack : MonoBehaviour
{
    [SerializeField] private GameObject _katana;
    [SerializeField] private Collider _katanaCollider;
    
    private Player _player;
    private Animator _animator;

    private void Awake()
    {
        _player = GetComponentInParent<Player>();        // 부모의 Player
        _animator = GetComponentInChildren<Animator>();  // Goku 자신의 Animator
    }
    
    private void OnEnable()
    {
        // 구독: 무기 모드가 바뀌면 불러 달라고 등록한다.
        _player.OnWeaponModeChanged += RefreshKatana;
    }

    private void OnDisable()
    {
        // 구독 취소
        _player.OnWeaponModeChanged -= RefreshKatana;
    }
    
    private void Start()
    {
        RefreshKatana(_player.WeaponMode);  // 처음 모드에 맞춰 카타나를 보이거나 숨긴다.
    }
    
    private void RefreshKatana(WeaponMode mode)
    {
        _katana.SetActive(mode == WeaponMode.Melee);
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }
        if (_player.WeaponMode != WeaponMode.Melee)
        {
            return; // 근접 모드에서만 휘두른다.
        }

        if (Input.GetButtonDown("Fire2"))
        {
            _animator.SetTrigger("Slash");
        }
    }
    
    // 칼날을 켠다. slash8의 ActivateCollider 이벤트가 불러 준다.
    public void ActivateCollider()
    {
        Debug.Log("카타나 콜라이더 활성화");
        _katanaCollider.enabled = true;
    }

    // 칼날을 끈다. slash8의 DeactivateCollider 이벤트가 불러 준다.
    public void DeactivateCollider()
    {
        Debug.Log("카타나 콜라이더 비활성화");
        _katanaCollider.enabled = false;
    }
}
