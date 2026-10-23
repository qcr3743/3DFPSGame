using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UI_HitEffect : MonoBehaviour
{
    [SerializeField] private float _flashTime = 0.2f;

    private Image _image;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _image.enabled = false;
    }

    public void Flash()
    {
        StartCoroutine(Flash_Coroutine());
    }

    public void Fade()
    {
        // 1. 0.4로 보이게한다
        // 2. 0.4->0으로 서서히 사라지게함
    }

    public IEnumerator Flash_Coroutine()
    {
        _image.enabled = true;
        yield return new WaitForSeconds(_flashTime);
        _image.enabled = false;
    }
}