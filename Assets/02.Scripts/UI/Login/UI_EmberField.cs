using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 지평선 근처에서 불씨(주황)와 재(회색)가 피어올라 오른쪽 위로 흔들리며 떠오르다 사라진다.
// 파티클 시스템 대신 UI Image를 미리 만들어 두고 돌려 쓴다 (오브젝트 풀링).
public class UI_EmberField : MonoBehaviour
{
    [Header("이미지")]
    [SerializeField] private Sprite _sprite;
    [SerializeField] private int _poolSize = 40;

    [Header("생성 영역 (1920x1080 좌상단 기준 px)")]
    [SerializeField] private Vector2 _referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] private Vector2 _spawnMin = new Vector2(1300f, 380f);
    [SerializeField] private Vector2 _spawnMax = new Vector2(1700f, 460f);

    [Header("생성")]
    [SerializeField] private float _spawnInterval = 0.12f;
    [SerializeField, Range(0f, 1f)] private float _emberRatio = 0.7f;   // 나머지는 재

    [Header("수명 / 움직임 (px)")]
    [SerializeField] private float _minLifetime = 3f;
    [SerializeField] private float _maxLifetime = 6f;
    [SerializeField] private Vector2 _minVelocity = new Vector2(6f, 18f);    // x = 오른쪽, y = 위쪽 (초당 px)
    [SerializeField] private Vector2 _maxVelocity = new Vector2(22f, 40f);
    [SerializeField] private float _wobbleAmplitude = 6f;
    [SerializeField] private float _minWobbleFrequency = 0.6f;
    [SerializeField] private float _maxWobbleFrequency = 1.6f;
    [SerializeField, Range(0f, 0.5f)] private float _fadeInRatio = 0.15f;   // 수명 앞부분 페이드 인 비율
    [SerializeField, Range(0f, 1f)] private float _fadeOutRatio = 0.4f;     // 수명 뒷부분 페이드 아웃 비율

    [Header("불씨")]
    [SerializeField] private Color _emberColor = new Color(1f, 0.6f, 0.24f, 0.9f);
    [SerializeField] private Vector2 _emberSizeRange = new Vector2(2f, 4f);
    [SerializeField] private float _emberTwinkleSpeed = 8f;   // 불씨만 밝기가 살짝 떨린다

    [Header("재")]
    [SerializeField] private Color _ashColor = new Color(0.6f, 0.62f, 0.6f, 0.5f);
    [SerializeField] private Vector2 _ashSizeRange = new Vector2(3f, 5f);

    private class Particle
    {
        public RectTransform Rect;
        public Image Image;
        public Color Color;
        public Vector2 Velocity;
        public float Age;
        public float Lifetime;
        public float WobblePhase;
        public float WobbleFrequency;
        public bool IsEmber;
        public bool IsAlive;
    }

    private readonly List<Particle> _particles = new List<Particle>();
    private float _spawnTimer;

    private void Awake()
    {
        for (int i = 0; i < _poolSize; i++)
        {
            var go = new GameObject("Ember_" + i, typeof(RectTransform), typeof(Image));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);

            var image = go.GetComponent<Image>();
            image.sprite = _sprite;
            image.raycastTarget = false;
            go.SetActive(false);

            _particles.Add(new Particle { Rect = (RectTransform)go.transform, Image = image });
        }
    }

    private void OnDisable()
    {
        foreach (Particle particle in _particles)
        {
            particle.IsAlive = false;
            particle.Rect.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;

        _spawnTimer += deltaTime;
        while (_spawnInterval > 0f && _spawnTimer >= _spawnInterval)
        {
            _spawnTimer -= _spawnInterval;
            Spawn();
        }

        foreach (Particle particle in _particles)
        {
            if (particle.IsAlive)
            {
                UpdateParticle(particle, deltaTime);
            }
        }
    }

    private void Spawn()
    {
        Particle particle = GetFreeParticle();
        if (particle == null)
        {
            return;   // 풀이 다 쓰이고 있으면 이번 생성은 건너뛴다
        }

        particle.IsEmber = Random.value < _emberRatio;
        particle.Color = particle.IsEmber ? _emberColor : _ashColor;

        Vector2 sizeRange = particle.IsEmber ? _emberSizeRange : _ashSizeRange;
        float size = Random.Range(sizeRange.x, sizeRange.y);

        particle.Age = 0f;
        particle.Lifetime = Random.Range(_minLifetime, _maxLifetime);
        particle.Velocity = new Vector2(Random.Range(_minVelocity.x, _maxVelocity.x),
                                        Random.Range(_minVelocity.y, _maxVelocity.y));
        particle.WobblePhase = Random.Range(0f, Mathf.PI * 2f);
        particle.WobbleFrequency = Random.Range(_minWobbleFrequency, _maxWobbleFrequency);

        // 생성 지점을 정규화 앵커로 두고, 이동은 anchoredPosition(px)으로 한다.
        float x = Random.Range(_spawnMin.x, _spawnMax.x);
        float y = Random.Range(_spawnMin.y, _spawnMax.y);
        Vector2 anchor = new Vector2(x / _referenceResolution.x, 1f - y / _referenceResolution.y);

        particle.Rect.anchorMin = anchor;
        particle.Rect.anchorMax = anchor;
        particle.Rect.sizeDelta = new Vector2(size, size);
        particle.Rect.anchoredPosition = Vector2.zero;

        particle.IsAlive = true;
        particle.Image.color = WithAlpha(particle.Color, 0f);
        particle.Rect.gameObject.SetActive(true);
    }

    private Particle GetFreeParticle()
    {
        foreach (Particle particle in _particles)
        {
            if (!particle.IsAlive)
            {
                return particle;
            }
        }
        return null;
    }

    private void UpdateParticle(Particle particle, float deltaTime)
    {
        particle.Age += deltaTime;
        if (particle.Age >= particle.Lifetime)
        {
            particle.IsAlive = false;
            particle.Rect.gameObject.SetActive(false);
            return;
        }

        float age = particle.Age;
        float wobble = Mathf.Sin(particle.WobblePhase + age * particle.WobbleFrequency * Mathf.PI * 2f) * _wobbleAmplitude;
        particle.Rect.anchoredPosition = particle.Velocity * age + new Vector2(wobble, 0f);

        // 수명에 따른 페이드 인/아웃
        float life = age / particle.Lifetime;
        float fadeIn = _fadeInRatio > 0f ? Mathf.Clamp01(life / _fadeInRatio) : 1f;
        float fadeOut = _fadeOutRatio > 0f ? Mathf.Clamp01((1f - life) / _fadeOutRatio) : 1f;
        float alpha = particle.Color.a * fadeIn * fadeOut;

        if (particle.IsEmber)
        {
            alpha *= 0.75f + 0.25f * Mathf.Sin(particle.WobblePhase * 3f + age * _emberTwinkleSpeed);
        }

        particle.Image.color = WithAlpha(particle.Color, alpha);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
