using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 배경 생동감 연출 (BG_Image > BG_Ambience). 로그인 기능과 무관한 장식만 있다.
// BG_Image의 자식이라 UI_BackgroundDrift 확대와 같이 움직이고, BG_LeftShade보다 먼저 그려져 왼쪽은 계속 어둡다.
// 위치는 시안 좌표(1920x1080, 좌상단 px)를 정규화 앵커로 바꿔 넣어, 해상도가 달라도 배경 이미지의 같은 지점에 붙는다.
public static partial class LoginSceneBuilder
{
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;

    private static Sprite _spriteGlow;
    private static Sprite _spriteBeam;
    private static Sprite _spriteFog;

    // CreateSprites()에서 호출한다.
    private static void CreateAmbienceSprites()
    {
        // 부드러운 원형 glow (가로등, 연막탄, 폭발 섬광 공용)
        _spriteGlow = GetOrCreateSprite("Login_Glow.png", 128, 128, GlowPixel,
            Vector4.zero, TextureWrapMode.Clamp, FilterMode.Bilinear);

        // 아래가 좁고 위로 퍼지며 사라지는 빛줄기
        _spriteBeam = GetOrCreateSprite("Login_Beam.png", 128, 256, BeamPixel,
            Vector4.zero, TextureWrapMode.Clamp, FilterMode.Bilinear);

        // 가로로 끊김 없이 반복되는 안개 노이즈 (RawImage uvRect 스크롤용이라 Repeat)
        _spriteFog = GetOrCreateSprite("Login_Fog.png", 512, 128, FogPixel,
            Vector4.zero, TextureWrapMode.Repeat, FilterMode.Bilinear);
    }

    private static void BuildAmbience(RectTransform bgImage, RectTransform bgRoot)
    {
        RectTransform ambience = CreateRect("BG_Ambience", bgImage);
        Stretch(ambience);

        // 아래에 만든 것이 위에 그려진다: 서치라이트 → 조명 glow → 폭발 → 불씨 → 안개

        // ───── 서치라이트 (피벗 1250, 455 / 빛줄기 아래 끝) ─────
        Image beam = CreateImage("Searchlight", ambience, _spriteBeam, new Color(1f, 1f, 1f, 0.12f), false);
        PlaceAt(beam.rectTransform, 1250f, 455f, 340f, 780f, new Vector2(0.5f, 0f));
        beam.gameObject.AddComponent<UI_SearchlightSweep>();

        // ───── 가로등 glow: 느리게 일렁이다 가끔 0.1초 꺼짐 ─────
        Image streetlight = CreateGlow(ambience, "Glow_Streetlight", 1255f, 115f, 220f, Hex("#FFB060"));
        var streetlightFlicker = streetlight.gameObject.AddComponent<UI_LightFlicker>();
        SetFloatField(streetlightFlicker, "_minAlpha", 0.4f);
        SetFloatField(streetlightFlicker, "_maxAlpha", 0.55f);
        SetFloatField(streetlightFlicker, "_noiseSpeed", 1.2f);
        SetFloatField(streetlightFlicker, "_outChancePerSecond", 0.2f);
        SetFloatField(streetlightFlicker, "_outDuration", 0.1f);

        // ───── 연막탄 glow: 빠르게 일렁임, 꺼짐 없음 ─────
        Image smoke = CreateGlow(ambience, "Glow_Smoke", 1536f, 690f, 260f, Accent);
        var smokeFlicker = smoke.gameObject.AddComponent<UI_LightFlicker>();
        SetFloatField(smokeFlicker, "_minAlpha", 0.35f);
        SetFloatField(smokeFlicker, "_maxAlpha", 0.6f);
        SetFloatField(smokeFlicker, "_noiseSpeed", 4f);
        SetFloatField(smokeFlicker, "_outChancePerSecond", 0f);

        // ───── 멀리서 터지는 폭발 (위치는 UI_DistantExplosion이 매번 정함) ─────
        Image explosion = CreateGlow(ambience, "Explosion_Flash", 1395f, 440f, 300f, WithAlpha(Hex("#FF8A3C"), 0f));
        var distantExplosion = explosion.gameObject.AddComponent<UI_DistantExplosion>();
        SetField(distantExplosion, "_flash", explosion);
        SetField(distantExplosion, "_shakeTarget", bgRoot);
        SetField(distantExplosion, "_audioSource", _uiAudio);

        // ───── 불씨 / 재 (실행 시 40개를 풀로 만든다) ─────
        RectTransform embers = CreateRect("Embers", ambience);
        Stretch(embers);
        var emberField = embers.gameObject.AddComponent<UI_EmberField>();
        SetField(emberField, "_sprite", _spriteDot);

        // ───── 안개 (화면 아래 절반, y 540~1080) ─────
        RectTransform fog = CreateRect("Fog", ambience);
        fog.anchorMin = new Vector2(0f, 0f);
        fog.anchorMax = new Vector2(1f, 0.5f);
        fog.pivot = new Vector2(0.5f, 0.5f);
        fog.offsetMin = Vector2.zero;
        fog.offsetMax = Vector2.zero;

        Color fogColor = Hex("#B8C4C0");
        CreateFogLayer(fog, "Fog_Back", WithAlpha(fogColor, 0.05f), 0.006f, 1.3f);
        CreateFogLayer(fog, "Fog_Front", WithAlpha(fogColor, 0.08f), 0.014f, 0.9f);
    }

    // ───── 배치 헬퍼 ─────

    // 시안 좌표(x, y)를 부모 기준 정규화 앵커로 바꾸고, 크기는 px로 둔다.
    private static void PlaceAt(RectTransform rect, float x, float y, float width, float height, Vector2 pivot)
    {
        Vector2 anchor = new Vector2(x / ReferenceWidth, 1f - y / ReferenceHeight);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, height);
    }

    // 중심 (x, y), 지름 diameter인 glow
    private static Image CreateGlow(Transform parent, string name, float x, float y, float diameter, Color color)
    {
        Image glow = CreateImage(name, parent, _spriteGlow, color, false);
        PlaceAt(glow.rectTransform, x, y, diameter, diameter, new Vector2(0.5f, 0.5f));
        return glow;
    }

    private static void CreateFogLayer(Transform parent, string name, Color color, float scrollSpeed, float tiling)
    {
        if (_spriteFog == null)
        {
            Debug.LogWarning($"[LoginSceneBuilder] 안개 텍스처가 없어 {name}을(를) 만들지 않았습니다.");
            return;
        }

        RectTransform rect = CreateRect(name, parent);
        Stretch(rect);

        var rawImage = rect.gameObject.AddComponent<RawImage>();
        rawImage.texture = _spriteFog.texture;
        rawImage.color = color;
        rawImage.raycastTarget = false;
        rawImage.uvRect = new Rect(0f, 0f, tiling, 1f);

        var scroll = rect.gameObject.AddComponent<UI_FogScroll>();
        SetField(scroll, "_rawImage", rawImage);
        SetFloatField(scroll, "_scrollSpeed", scrollSpeed);
        SetFloatField(scroll, "_tiling", tiling);
    }

    private static void SetFloatField(Object target, string fieldName, float value)
    {
        var serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(fieldName);
        if (property == null || property.propertyType != SerializedPropertyType.Float)
        {
            Debug.LogError($"[LoginSceneBuilder] {target.GetType().Name}.{fieldName} float 필드를 찾을 수 없습니다.");
            return;
        }

        property.floatValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // ───── 픽셀 함수 ─────

    // 중심이 밝고 가장자리로 부드럽게 사라지는 원
    private static Color GlowPixel(int x, int y, int width, int height)
    {
        float u = (x + 0.5f) / width * 2f - 1f;
        float v = (y + 0.5f) / height * 2f - 1f;
        float distance = Mathf.Clamp01(Mathf.Sqrt(u * u + v * v));

        float falloff = 1f - distance;
        return new Color(1f, 1f, 1f, falloff * falloff);
    }

    // 아래(광원)는 좁고 밝으며, 위로 갈수록 넓어지고 사라진다. 좌우 가장자리도 부드럽게.
    private static Color BeamPixel(int x, int y, int width, int height)
    {
        float v = (y + 0.5f) / height;                                  // 0 = 아래(광원), 1 = 위
        float u = Mathf.Abs((x + 0.5f) / width * 2f - 1f);              // 0 = 가운데, 1 = 좌우 끝

        float halfWidth = Mathf.Lerp(0.06f, 1f, v);
        float horizontal = Mathf.Clamp01(1f - u / halfWidth);
        horizontal = horizontal * horizontal * (3f - 2f * horizontal);

        float vertical = Mathf.Pow(1f - v, 1.3f) * Mathf.Clamp01(v / 0.03f);
        return new Color(1f, 1f, 1f, horizontal * vertical);
    }

    // 가로로 이어지는(tileable) 값 노이즈를 여러 겹 쌓고, 위쪽은 투명하게 사라지게 한다.
    private static Color FogPixel(int x, int y, int width, int height)
    {
        float u = (float)x / width;
        float v = (float)y / height;   // 0 = 아래, 1 = 위

        float noise = 0f;
        float amplitude = 0.5f;
        float total = 0f;
        for (int octave = 0; octave < 4; octave++)
        {
            int cellsX = 4 << octave;
            int cellsY = 2 << octave;
            noise += TileableValueNoise(u * cellsX, v * cellsY, cellsX, cellsY, octave) * amplitude;
            total += amplitude;
            amplitude *= 0.5f;
        }
        noise /= total;

        // 대비를 올려서 덩어리진 안개로 만든다.
        float density = Mathf.Clamp01(Mathf.InverseLerp(0.3f, 0.8f, noise));
        density = density * density * (3f - 2f * density);

        // 위쪽 가장자리는 투명 (v 1 → 0.4 구간에서 서서히 나타남)
        float fade = Mathf.InverseLerp(1f, 0.4f, v);
        fade = fade * fade * (3f - 2f * fade);

        return new Color(1f, 1f, 1f, density * fade);
    }

    // 격자 좌표를 period로 감싸서 오른쪽 끝과 왼쪽 끝이 이어지는 값 노이즈
    private static float TileableValueNoise(float x, float y, int periodX, int periodY, int seed)
    {
        int x0 = Mathf.FloorToInt(x);
        int y0 = Mathf.FloorToInt(y);
        float fx = x - x0;
        float fy = y - y0;

        float sx = fx * fx * (3f - 2f * fx);
        float sy = fy * fy * (3f - 2f * fy);

        float a = LatticeValue(x0, y0, periodX, periodY, seed);
        float b = LatticeValue(x0 + 1, y0, periodX, periodY, seed);
        float c = LatticeValue(x0, y0 + 1, periodX, periodY, seed);
        float d = LatticeValue(x0 + 1, y0 + 1, periodX, periodY, seed);

        return Mathf.Lerp(Mathf.Lerp(a, b, sx), Mathf.Lerp(c, d, sx), sy);
    }

    private static float LatticeValue(int x, int y, int periodX, int periodY, int seed)
    {
        x = ((x % periodX) + periodX) % periodX;
        y = ((y % periodY) + periodY) % periodY;

        unchecked
        {
            int hash = x * 374761393 + y * 668265263 + seed * 982451653;
            hash = (hash ^ (hash >> 13)) * 1274126177;
            hash ^= hash >> 16;
            return (hash & 0x7fffffff) / (float)int.MaxValue;
        }
    }
}
