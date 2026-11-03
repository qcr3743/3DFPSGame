using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 배경 클릭 사격 미니 연출 (로그인 기능과 무관).
//  - ShootCatcher : BG 바로 위, 패널보다 뒤의 화면 전체 투명 Image (입력 받는 영역)
//  - ShootFX      : 패널/HUD보다 앞, FX_Overlay보다 뒤. 아래 순서로 그려진다 (전부 Raycast OFF)
//                   탄도선 → 총구(glow, 별 섬광) → +1 KILL → 크로스헤어 → 히트마커
public static partial class LoginSceneBuilder
{
    // 디자인 레이아웃 폴더의 원본 크로스헤어. Texture Type이 Default라 import 설정을 바꾸지 않고 RawImage로 쓴다.
    private const string CrosshairPath = "Assets/UI디자인레이아웃/02_크로스헤어/IMG_Crosshair.png";
    private const float CrosshairSize = 48f;
    private const float HitMarkerSize = 39f;          // 26px의 1.5배
    private const float MuzzleFlashSize = 140f;
    private const float MuzzleGlowSize = 320f;
    private const float MuzzleGlowAlpha = 0.35f;
    private const float KillTextFontSize = 18f;

    private static Sprite _spriteHitMarker;
    private static Sprite _spriteMuzzleFlash;

    // BuildCanvas에서 UI_IntroSequence를 만든 뒤 호출한다 (다른 오브젝트를 이름으로 찾아 연결).
    private static void BuildShooting(RectTransform canvasRect, UI_IntroSequence intro)
    {
        _spriteHitMarker = GetOrCreateSprite("Login_HitMarker.png", 32, 32, HitMarkerPixel,
            Vector4.zero, TextureWrapMode.Clamp, FilterMode.Bilinear);
        _spriteMuzzleFlash = GetOrCreateSprite("Login_MuzzleFlash.png", 128, 128, MuzzleFlashPixel,
            Vector4.zero, TextureWrapMode.Clamp, FilterMode.Bilinear);

        var bgRoot = canvasRect.Find("BG") as RectTransform;
        Transform bracket = canvasRect.Find("TargetBracket");
        Transform fxOverlay = canvasRect.Find("FX_Overlay");
        UI_TargetBracketFeel bracketFeel = bracket != null ? bracket.GetComponent<UI_TargetBracketFeel>() : null;

        if (bgRoot == null || bracketFeel == null || fxOverlay == null)
        {
            Debug.LogError("[LoginSceneBuilder] 사격 연출에 필요한 오브젝트(BG, TargetBracket+UI_TargetBracketFeel, FX_Overlay)를 찾지 못했습니다.");
            return;
        }

        // ───── ShootCatcher: BG 바로 다음 (패널·버튼·입력칸보다 뒤) ─────
        Image catcher = CreateImage("ShootCatcher", canvasRect, null, new Color(1f, 1f, 1f, 0f), true);
        Stretch(catcher.rectTransform);
        catcher.rectTransform.SetSiblingIndex(bgRoot.GetSiblingIndex() + 1);

        // ───── ShootFX: FX_Overlay 바로 앞 (다른 UI보다 위에 그려짐) ─────
        RectTransform fx = CreateRect("ShootFX", canvasRect);
        Stretch(fx);
        fx.SetSiblingIndex(fxOverlay.GetSiblingIndex());   // 이 자리에 끼워 넣으면 FX_Overlay는 한 칸 뒤로 밀린다

        UI_ShotTracer tracer = CreateTracer(fx);
        UI_MuzzleFlash muzzleFlash = CreateMuzzleFlash(fx);
        UI_FloatingTextPool killTexts = CreateKillTextPool(fx);
        RawImage crosshair = CreateCrosshair(fx);

        Image hitMarker = CreateImage("Shoot_HitMarker", fx, _spriteHitMarker, new Color(1f, 1f, 1f, 0f), false);
        SetCentered(hitMarker.rectTransform, HitMarkerSize);

        // ───── 스크립트 연결 ─────
        var shooter = catcher.gameObject.AddComponent<UI_ShootCatcher>();
        SetField(shooter, "_fxRoot", fx);
        SetField(shooter, "_crosshair", crosshair);
        SetField(shooter, "_hitMarker", hitMarker);
        SetField(shooter, "_muzzleFlash", muzzleFlash);
        SetField(shooter, "_tracer", tracer);
        SetField(shooter, "_targetBracket", bracketFeel);
        SetField(shooter, "_killTexts", killTexts);
        SetField(shooter, "_shakeTarget", bgRoot);
        SetField(shooter, "_intro", intro);
        SetField(shooter, "_audioSource", _uiAudio);
    }

    // 총구 → 클릭 지점 탄도선 (두께 2px, 연노랑)
    private static UI_ShotTracer CreateTracer(Transform parent)
    {
        Image line = CreateImage("Shoot_Tracer", parent, null, WithAlpha(Hex("#FFF2A8"), 0f), false);
        RectTransform rect = line.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);   // 명중 지점 쪽 끝 기준으로 줄어든다
        rect.sizeDelta = new Vector2(0f, 2f);

        var tracer = line.gameObject.AddComponent<UI_ShotTracer>();
        SetField(tracer, "_line", line);
        SetFloatField(tracer, "_thickness", 2f);
        return tracer;
    }

    // 총구 불꽃: 주황 glow(뒤) + 별 모양 섬광(앞). 위치는 UI_ShootCatcher가 발사 때마다 정한다.
    private static UI_MuzzleFlash CreateMuzzleFlash(Transform parent)
    {
        RectTransform root = CreateRect("Shoot_Muzzle", parent);
        SetCentered(root, 0f);

        Image glow = CreateImage("Glow", root, _spriteGlow, WithAlpha(Hex("#FF9A3C"), 0f), false);
        SetCentered(glow.rectTransform, MuzzleGlowSize);

        Image flash = CreateImage("Flash", root, _spriteMuzzleFlash, WithAlpha(Hex("#FFD27A"), 0f), false);
        SetCentered(flash.rectTransform, MuzzleFlashSize);

        var muzzle = root.gameObject.AddComponent<UI_MuzzleFlash>();
        SetField(muzzle, "_root", root);
        SetField(muzzle, "_flash", flash);
        SetField(muzzle, "_glow", glow);
        SetFloatField(muzzle, "_glowAlpha", MuzzleGlowAlpha);
        return muzzle;
    }

    // "+1 KILL" 풀: 비활성 템플릿 하나를 두고 실행 시 복제한다.
    private static UI_FloatingTextPool CreateKillTextPool(Transform parent)
    {
        RectTransform root = CreateRect("Shoot_KillTexts", parent);
        Stretch(root);

        TextMeshProUGUI template = CreateText("KillText", root, "+1 KILL", _fontMono, KillTextFontSize, Accent,
            TextAlignmentOptions.Center, FontStyles.Bold, 2f);
        RectTransform templateRect = template.rectTransform;
        templateRect.anchorMin = new Vector2(0.5f, 0.5f);
        templateRect.anchorMax = new Vector2(0.5f, 0.5f);
        templateRect.pivot = new Vector2(0.5f, 0.5f);
        templateRect.sizeDelta = new Vector2(200f, 30f);
        template.gameObject.SetActive(false);

        var pool = root.gameObject.AddComponent<UI_FloatingTextPool>();
        SetField(pool, "_template", template);
        return pool;
    }

    private static RawImage CreateCrosshair(Transform parent)
    {
        RectTransform rect = CreateRect("Shoot_Crosshair", parent);
        SetCentered(rect, CrosshairSize);

        var crosshair = rect.gameObject.AddComponent<RawImage>();
        crosshair.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(CrosshairPath);
        crosshair.color = new Color(1f, 1f, 1f, 0f);   // 마우스가 ShootCatcher 위에 있을 때만 0.8로 보인다
        crosshair.raycastTarget = false;               // 켜 두면 크로스헤어가 이벤트를 가로채 Enter/Exit가 깜빡인다

        if (crosshair.texture == null)
        {
            Debug.LogWarning($"[LoginSceneBuilder] 크로스헤어 이미지가 없습니다: {CrosshairPath}");
        }

        return crosshair;
    }

    // 앵커·피벗을 가운데로 두고 정사각형 크기 지정 (UI_ShootCatcher가 anchoredPosition으로 옮김)
    private static void SetCentered(RectTransform rect, float size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(size, size);
    }

    // ───── 픽셀 함수 ─────

    // 히트마커 X: 두 대각선, 가운데는 비워 둔다 (크로스헤어 중심이 보이게).
    private static Color HitMarkerPixel(int x, int y, int width, int height)
    {
        const float sqrt2 = 1.41421356f;
        const float halfThickness = 1.25f;
        const float innerRadius = 5f;

        float cx = x + 0.5f - width * 0.5f;
        float cy = y + 0.5f - height * 0.5f;
        float outerRadius = width * 0.5f - 1f;

        float diagonalDistance = Mathf.Min(Mathf.Abs(cx - cy), Mathf.Abs(cx + cy)) / sqrt2;
        float line = Mathf.Clamp01(halfThickness - diagonalDistance + 0.5f);

        float radius = Mathf.Sqrt(cx * cx + cy * cy);
        float inner = Mathf.Clamp01(radius - innerRadius + 0.5f);
        float outer = Mathf.Clamp01(outerRadius - radius + 0.5f);

        return new Color(1f, 1f, 1f, line * inner * outer);
    }

    // 총구 섬광 별: 긴 가시 4개 + 짧은 가시 4개, 가운데는 밝게. 색은 Image color(#FFD27A)로 입힌다.
    private static Color MuzzleFlashPixel(int x, int y, int width, int height)
    {
        float u = (x + 0.5f) / width * 2f - 1f;
        float v = (y + 0.5f) / height * 2f - 1f;
        float radius = Mathf.Sqrt(u * u + v * v);
        float angle = Mathf.Atan2(v, u);

        // 각도에 따라 별의 가장자리 반지름이 바뀐다 (가시 방향일수록 멀리 뻗음)
        float longSpike = Mathf.Pow(Mathf.Abs(Mathf.Cos(angle * 2f)), 12f);
        float shortSpike = 0.55f * Mathf.Pow(Mathf.Abs(Mathf.Sin(angle * 2f)), 12f);
        float edge = 0.16f + 0.84f * Mathf.Max(longSpike, shortSpike);

        float star = Mathf.Clamp01((edge - radius) / edge);
        star = Mathf.Pow(star, 1.5f);

        // 가운데 둥근 코어
        float core = Mathf.Clamp01(1f - radius / 0.28f);
        core *= core;

        return new Color(1f, 1f, 1f, Mathf.Clamp01(star + core));
    }
}
