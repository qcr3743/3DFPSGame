using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 로그인 화면 배경 클릭 사격 미니 연출. (로그인 기능과 무관)
// 화면 전체를 덮는 투명 Image(ShootCatcher, 패널보다 뒤)에 붙인다.
// 패널·버튼·입력칸은 이보다 앞에 있어서 그 위에서는 이벤트가 여기까지 오지 않는다 → 사격되지 않음.
//
// 이 스크립트는 입력과 조율만 맡고, 각 연출은 전용 컴포넌트가 한다.
//  - UI_MuzzleFlash      : 총구 불꽃 + glow
//  - UI_ShotTracer       : 탄도선
//  - UI_TargetBracketFeel: 조준 모서리 반응 + HOSTILE 카운트
//  - UI_FloatingTextPool : "+1 KILL"
public class UI_ShootCatcher : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("참조")]
    [SerializeField] private RectTransform _fxRoot;               // 크로스헤어/연출이 들어 있는 화면 전체 컨테이너
    [SerializeField] private Graphic _crosshair;                  // 마우스를 따라가는 크로스헤어 (RawImage)
    [SerializeField] private Graphic _hitMarker;                  // 명중 시 X 표시
    [SerializeField] private UI_MuzzleFlash _muzzleFlash;
    [SerializeField] private UI_ShotTracer _tracer;
    [SerializeField] private UI_TargetBracketFeel _targetBracket;  // 명중 판정 영역 겸 카운트
    [SerializeField] private UI_FloatingTextPool _killTexts;
    [SerializeField] private RectTransform _shakeTarget;          // 흔들 배경 루트 (BG)
    [SerializeField] private UI_IntroSequence _intro;             // 인트로 중에는 사격 비활성

    [Header("사격")]
    [SerializeField] private float _fireInterval = 0.12f;         // 연사 간격 (누르고 있으면 계속 발사)
    [SerializeField] private Vector2 _referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField] private Vector2 _muzzlePosition = new Vector2(1780f, 1040f);   // 좌상단 기준 px (화면 오른쪽 아래)

    [Header("흔들림")]
    [SerializeField] private float _shakeDuration = 0.08f;
    [SerializeField] private float _minShake = 1f;
    [SerializeField] private float _maxShake = 2f;

    [Header("크로스헤어 / 히트마커")]
    [SerializeField] private float _crosshairAlpha = 0.8f;
    [SerializeField] private float _hitMarkerDuration = 0.2f;
    [SerializeField] private Color _hitMarkerColor = Color.white;
    [SerializeField] private Color _lastKillHitMarkerColor = new Color(1f, 0.353f, 0.302f, 1f);   // #FF5A4D

    [Header("히트스톱 (탄도선·흔들림만 멈춤)")]
    [SerializeField] private float _hitStopDuration = 0.04f;

    [Header("효과음 (비어 있으면 무시)")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _shotClip;
    [SerializeField] private AudioClip _hitClip;

    private Camera _canvasCamera;
    private RectTransform _targetArea;
    private Vector2 _shakeBasePosition;

    private bool _isHovered;
    private bool _isHolding;
    private float _fireCooldown;

    private float _hitMarkerTimer;
    private float _shakeTimer;
    private float _shakeStrength;
    private float _hitStopTimer;

    private bool IsIntroPlaying => _intro != null && _intro.IsPlaying;

    private void Awake()
    {
        // Screen Space - Camera 캔버스는 화면 좌표 → UI 좌표 변환에 캔버스 카메라가 필요하다.
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas = canvas.rootCanvas;
            _canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        if (_targetBracket != null)
        {
            _targetArea = _targetBracket.transform as RectTransform;
        }

        if (_shakeTarget != null)
        {
            _shakeBasePosition = _shakeTarget.anchoredPosition;
        }
    }

    private void OnEnable()
    {
        SetAlpha(_hitMarker, 0f);
        SetCrosshairVisible(false);
    }

    private void OnDisable()
    {
        // 꺼질 때 시스템 커서와 흔들림을 원래대로 돌려 둔다.
        _isHovered = false;
        _isHolding = false;
        SetCrosshairVisible(false);
        if (_shakeTarget != null)
        {
            _shakeTarget.anchoredPosition = _shakeBasePosition;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHovered = true;
        SetCrosshairVisible(!IsIntroPlaying);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 패널/버튼 위로 올라가면 여기서 빠져나간 것으로 처리 → 시스템 커서 복귀
        _isHovered = false;
        _isHolding = false;
        SetCrosshairVisible(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            _isHolding = true;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            _isHolding = false;
        }
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;

        if (IsIntroPlaying)
        {
            _isHolding = false;
            SetCrosshairVisible(false);
            return;
        }

        if (_isHovered)
        {
            SetCrosshairVisible(true);
            MoveToScreenPoint(_crosshair, Input.mousePosition);
        }

        _fireCooldown -= deltaTime;
        if (_isHovered && _isHolding && _fireCooldown <= 0f)
        {
            _fireCooldown = _fireInterval;
            Fire(Input.mousePosition);
        }

        UpdateHitMarker(deltaTime);
        UpdateShake(deltaTime);
    }

    private void Fire(Vector2 screenPoint)
    {
        if (!TryGetLocalPoint(screenPoint, out Vector2 hitPoint))
        {
            return;
        }

        Vector2 muzzlePoint = GetMuzzleLocalPoint();

        if (_muzzleFlash != null)
        {
            _muzzleFlash.Play(muzzlePoint);
        }

        if (_tracer != null)
        {
            _tracer.Play(muzzlePoint, hitPoint);
        }

        _shakeTimer = _shakeDuration;
        _shakeStrength = Random.Range(_minShake, _maxShake);
        PlayClip(_shotClip);

        if (_targetArea != null && RectTransformUtility.RectangleContainsScreenPoint(_targetArea, screenPoint, _canvasCamera))
        {
            Hit(hitPoint);
        }
    }

    private void Hit(Vector2 hitPoint)
    {
        bool isLastKill = _targetBracket != null && _targetBracket.RegisterHit();

        // 히트마커: 마지막 처치는 빨간색
        _hitMarkerTimer = _hitMarkerDuration;
        if (_hitMarker != null)
        {
            _hitMarker.rectTransform.anchoredPosition = hitPoint;
            Color color = isLastKill ? _lastKillHitMarkerColor : _hitMarkerColor;
            color.a = 1f;
            _hitMarker.color = color;
        }

        if (_killTexts != null)
        {
            _killTexts.Spawn(hitPoint);
        }

        // 히트스톱: 탄도선과 흔들림만 잠깐 멈춘다.
        _hitStopTimer = _hitStopDuration;
        if (_tracer != null)
        {
            _tracer.Freeze(_hitStopDuration);
        }

        PlayClip(_hitClip);
    }

    private void UpdateHitMarker(float deltaTime)
    {
        if (_hitMarkerTimer <= 0f)
        {
            return;
        }

        _hitMarkerTimer = Mathf.Max(0f, _hitMarkerTimer - deltaTime);
        SetAlpha(_hitMarker, _hitMarkerDuration > 0f ? _hitMarkerTimer / _hitMarkerDuration : 0f);
    }

    private void UpdateShake(float deltaTime)
    {
        if (_shakeTarget == null || _shakeTimer <= 0f)
        {
            return;
        }

        // 히트스톱 중에는 흔들린 위치 그대로 멈춘다.
        if (_hitStopTimer > 0f)
        {
            _hitStopTimer -= deltaTime;
            return;
        }

        _shakeTimer -= deltaTime;
        _shakeTarget.anchoredPosition = _shakeTimer > 0f
            ? _shakeBasePosition + Random.insideUnitCircle * _shakeStrength
            : _shakeBasePosition;
    }

    // 좌상단 px 기준 총구 위치를 _fxRoot 로컬 좌표(피벗 가운데 기준)로 바꾼다. 해상도가 달라도 같은 비율 위치.
    private Vector2 GetMuzzleLocalPoint()
    {
        Rect rect = _fxRoot.rect;
        float u = _muzzlePosition.x / _referenceResolution.x;
        float v = 1f - _muzzlePosition.y / _referenceResolution.y;
        return new Vector2(rect.xMin + rect.width * u, rect.yMin + rect.height * v);
    }

    private bool TryGetLocalPoint(Vector2 screenPoint, out Vector2 localPoint)
    {
        localPoint = Vector2.zero;
        return _fxRoot != null &&
               RectTransformUtility.ScreenPointToLocalPointInRectangle(_fxRoot, screenPoint, _canvasCamera, out localPoint);
    }

    // 대상은 앵커·피벗이 가운데여야 한다.
    private void MoveToScreenPoint(Graphic target, Vector2 screenPoint)
    {
        if (target != null && TryGetLocalPoint(screenPoint, out Vector2 localPoint))
        {
            target.rectTransform.anchoredPosition = localPoint;
        }
    }

    private void SetCrosshairVisible(bool visible)
    {
        Cursor.visible = !visible;
        SetAlpha(_crosshair, visible ? _crosshairAlpha : 0f);
    }

    private void PlayClip(AudioClip clip)
    {
        if (_audioSource != null && clip != null)
        {
            _audioSource.PlayOneShot(clip);
        }
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        if (graphic == null)
        {
            return;
        }

        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }
}
