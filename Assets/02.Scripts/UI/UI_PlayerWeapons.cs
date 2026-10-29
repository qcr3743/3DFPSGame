using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerWeapons : MonoBehaviour
{
    [SerializeField] private Image _normalWeaponImage;
    [SerializeField] private Image _sniperWeaponImage;
    [SerializeField] private TextMeshProUGUI _bulletCountText;
    [SerializeField] private TextMeshProUGUI _bombCountText;

    [SerializeField] private Player _player;
    [SerializeField] private PlayerGunFire _playerGunFire;
    [SerializeField] private PlayerBombFire _playerBombFire;


    private void Start()
    {
        RefreshWeaponImage(_player.WeaponMode);
    }

    private void OnEnable()
    {
        _player.OnWeaponModeChanged += RefreshWeaponImage;
        _playerGunFire.OnBulletCountChanged += RefreshBulletCountText;
        _playerBombFire.OnFired += RefreshBombCountText;
    }

    private void OnDisable()
    {
        _player.OnWeaponModeChanged -= RefreshWeaponImage;
        _playerGunFire.OnBulletCountChanged -= RefreshBulletCountText;
        _playerBombFire.OnFired -= RefreshBombCountText;
    }

    private void RefreshWeaponImage(WeaponMode mode)
    {
        _normalWeaponImage.enabled = (mode == WeaponMode.Normal);
        _sniperWeaponImage.enabled = (mode == WeaponMode.Sniper);
    }

    private void RefreshBulletCountText(int bulletCount, int maxBulletCount)
    {
        _bulletCountText.SetText("{0} / {1}", bulletCount, maxBulletCount);
    }

    private void RefreshBombCountText(int bombCount)
    {
        _bombCountText.SetText("폭탄 {0}", bombCount);
    }
}