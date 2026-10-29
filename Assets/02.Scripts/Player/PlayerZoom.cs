using UnityEngine;

// 저격 모드에서 마우스 오른쪽 버튼을 누르고 있는 동안 시야각을 좁혀 확대
[RequireComponent(typeof(Player))]
public class PlayerZoom : MonoBehaviour
{
    [SerializeField] private GameObject _crosshair;
    [SerializeField] private GameObject _sniperZoom;
    [SerializeField] private float _normalPov = 60f;
    [SerializeField] private float _sinperPov = 15f;

    private Player _player;

    private void Awake()
    {
        _player = GetComponent<Player>();
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        bool isZooming = _player.WeaponMode == WeaponMode.Sniper
                         && Input.GetMouseButton(1);

        _sniperZoom.SetActive(isZooming);
        _crosshair.SetActive(!isZooming);
        Camera.main.fieldOfView = isZooming ? _sinperPov : _normalPov;
    }
}