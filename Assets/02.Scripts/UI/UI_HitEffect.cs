using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UI_HitEffect : MonoBehaviour
{
    [SerializeField] private float _flashTime = 0.06f;
    [SerializeField] private float _fadeTime = 0.3f;

    private Image _image;

    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    private void Start()
    {
        _image.enabled = false;
        Player player = GameObject.FindAnyObjectByType<Player>();
        if (player != null)
        {
            player.OnDamaged += OnPlayerDamaged;
        }
    }

    private void OnPlayerDamaged()
    {
        Fade(0.6f);
    }

    public void Flash()
    {
        SetAlpha(0.4f);
        StartCoroutine(Flash_Coroutine());
    }

    private void SetAlpha(float alpha)
    {
        Color color = _image.color;
        color.a = alpha;
        _image.color = color;
    }

    public IEnumerator Flash_Coroutine()
    {
        _image.enabled = true;
        yield return new WaitForSeconds(_flashTime);
        _image.enabled = false;
    }

    public void Fade(float alpha)
    {
        SetAlpha(alpha);
        StartCoroutine(Fade_Coroutine());
    }

    private IEnumerator Fade_Coroutine()
    {
        _image.enabled = true;

        float elapsedTime = 0f;

        float startAlpha = _image.color.a;

        while (elapsedTime < _fadeTime)
        {
            float alpha = Mathf.Lerp(startAlpha, 0, elapsedTime / _fadeTime);

            SetAlpha(alpha);

            elapsedTime += Time.deltaTime;
            yield return null; // 한 프레임씩 휴식
        }

        SetAlpha(0f);
        _image.enabled = false;
    }
}