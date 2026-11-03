using System.IO;
using UnityEditor;
using UnityEngine;

// 코드로 Texture2D를 만들어 Assets/04.Images/Login/에 PNG로 저장하고 Sprite로 import한다.
// 이미 PNG가 있으면 다시 만들지 않고 재사용한다.
public static partial class LoginSceneBuilder
{
    private delegate Color PixelFunc(int x, int y, int width, int height);

    private const int ChamferTextureSize = 64;
    private const int ChamferCut = 18;          // 왼쪽 아래 · 오른쪽 위를 깎는 크기(px)
    private const int ChamferSliceBorder = 20;  // 9-slice 경계 (깎인 모서리가 늘어나지 않게 Cut보다 크게)

    // BG_LeftShade 가로 그라데이션 (왼쪽 → 오른쪽)
    private static readonly float[] ShadeStops = { 0f, 0.30f, 0.52f, 0.70f, 1f };
    private static readonly float[] ShadeAlphas = { 0.94f, 0.82f, 0.25f, 0f, 0f };

    private static Sprite _spriteLeftShade;
    private static Sprite _spriteVignette;
    private static Sprite _spriteScanline;
    private static Sprite _spriteChamfer;
    private static Sprite _spriteFrame;
    private static Sprite _spriteDot;
    private static Sprite _spriteWarning;
    private static Sprite _spriteBackground;   // BG_Login.jpg (없으면 null → BG_Image 비워 둠)

    private const string BackgroundPath = ImageFolder + "/BG_Login.jpg";

    private static void CreateSprites()
    {
        EnsureFolder(ImageFolder);

        _spriteBackground = LoadBackgroundSprite();

        _spriteLeftShade = GetOrCreateSprite("Login_LeftShade.png", 256, 4, LeftShadePixel,
            Vector4.zero, TextureWrapMode.Clamp, FilterMode.Bilinear);

        _spriteVignette = GetOrCreateSprite("Login_Vignette.png", 256, 256, VignettePixel,
            Vector4.zero, TextureWrapMode.Clamp, FilterMode.Bilinear);

        // Tiled Image가 한 장의 사각형으로 반복되도록 Repeat + Full Rect로 import한다.
        _spriteScanline = GetOrCreateSprite("Login_Scanline.png", 1, 3, ScanlinePixel,
            Vector4.zero, TextureWrapMode.Repeat, FilterMode.Point);

        _spriteChamfer = GetOrCreateSprite("Login_ButtonChamfer.png", ChamferTextureSize, ChamferTextureSize, ChamferPixel,
            new Vector4(ChamferSliceBorder, ChamferSliceBorder, ChamferSliceBorder, ChamferSliceBorder),
            TextureWrapMode.Clamp, FilterMode.Bilinear);

        // 1px 테두리 (Sliced + Fill Center OFF로 입력칸/외곽선 버튼 테두리에 사용)
        _spriteFrame = GetOrCreateSprite("Login_Frame.png", 8, 8, FramePixel,
            new Vector4(1, 1, 1, 1), TextureWrapMode.Clamp, FilterMode.Point);

        _spriteDot = GetOrCreateSprite("Login_Dot.png", 16, 16, DotPixel,
            Vector4.zero, TextureWrapMode.Clamp, FilterMode.Bilinear);

        // 폰트에 ⚠(U+26A0) 글리프가 없어서 경고 아이콘을 sprite로 만든다.
        _spriteWarning = GetOrCreateSprite("Login_Warning.png", 32, 32, WarningPixel,
            Vector4.zero, TextureWrapMode.Clamp, FilterMode.Bilinear);

        // 배경 연출용 (glow, 빛줄기, 안개) — LoginSceneBuilder.Ambience.cs
        CreateAmbienceSprites();
    }

    private static Sprite GetOrCreateSprite(string fileName, int width, int height, PixelFunc pixel,
                                            Vector4 border, TextureWrapMode wrapMode, FilterMode filterMode)
    {
        string path = ImageFolder + "/" + fileName;
        bool isNew = false;

        if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[y * width + x] = pixel(x, y, width, height);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();

            // 에디터의 작업 폴더는 프로젝트 루트이므로 "Assets/..." 상대 경로로 바로 쓸 수 있다.
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            isNew = true;
        }

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && (isNew || importer.textureType != TextureImporterType.Sprite))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;   // Canvas Reference Pixels Per Unit(100)과 맞춰 1텍셀 = 1px
            importer.spriteBorder = border;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = wrapMode;
            importer.filterMode = filterMode;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogError($"[LoginSceneBuilder] Sprite를 불러오지 못했습니다: {path}");
        }
        return sprite;
    }

    // 배경 이미지(1920x1080)를 Sprite (2D and UI), Max Size 2048, Compression High Quality로 설정한다.
    // 파일이 없으면 null을 돌려주고 BG_Image는 비워 둔다.
    private static Sprite LoadBackgroundSprite()
    {
        var importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[LoginSceneBuilder] 배경 이미지가 없어 BG_Image를 비워 둡니다: {BackgroundPath}");
            return null;
        }

        bool needsReimport =
            importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single ||
            importer.maxTextureSize != 2048 ||
            importer.textureCompression != TextureImporterCompression.CompressedHQ ||
            importer.mipmapEnabled;

        if (needsReimport)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.mipmapEnabled = false;   // UI 전용이라 밉맵 불필요
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
    }

    // ───── 픽셀 함수 ─────

    private static Color LeftShadePixel(int x, int y, int width, int height)
    {
        float u = width > 1 ? x / (float)(width - 1) : 0f;

        float alpha = ShadeAlphas[ShadeAlphas.Length - 1];
        for (int i = 0; i < ShadeStops.Length - 1; i++)
        {
            if (u <= ShadeStops[i + 1])
            {
                float t = Mathf.InverseLerp(ShadeStops[i], ShadeStops[i + 1], u);
                alpha = Mathf.Lerp(ShadeAlphas[i], ShadeAlphas[i + 1], t);
                break;
            }
        }

        return WithAlpha(Ground, alpha);
    }

    // 가운데는 투명, 가장자리로 갈수록 검정 alpha 0.7
    private static Color VignettePixel(int x, int y, int width, int height)
    {
        float u = (x + 0.5f) / width * 2f - 1f;
        float v = (y + 0.5f) / height * 2f - 1f;
        float distance = Mathf.Sqrt(u * u + v * v);

        float t = Mathf.InverseLerp(0.35f, 1.35f, distance);
        float smooth = t * t * (3f - 2f * t);
        return new Color(0f, 0f, 0f, 0.7f * smooth);
    }

    // 3줄 중 맨 위 1줄만 흰색 alpha 0.03
    private static Color ScanlinePixel(int x, int y, int width, int height)
    {
        return y == height - 1 ? new Color(1f, 1f, 1f, 0.03f) : Color.clear;
    }

    // 왼쪽 아래와 오른쪽 위 모서리를 45도로 깎은 사각형. 경계는 1px 안티에일리어싱.
    private static Color ChamferPixel(int x, int y, int width, int height)
    {
        const float sqrt2 = 1.41421356f;
        float bottomLeft = Mathf.Clamp01((x + y + 1f - ChamferCut) / sqrt2 + 0.5f);
        float topRight = Mathf.Clamp01(((width - 1 - x) + (height - 1 - y) + 1f - ChamferCut) / sqrt2 + 0.5f);
        return new Color(1f, 1f, 1f, Mathf.Min(bottomLeft, topRight));
    }

    private static Color FramePixel(int x, int y, int width, int height)
    {
        bool isEdge = x == 0 || y == 0 || x == width - 1 || y == height - 1;
        return isEdge ? Color.white : Color.clear;
    }

    private static Color DotPixel(int x, int y, int width, int height)
    {
        float radius = width * 0.5f;
        float dx = x + 0.5f - radius;
        float dy = y + 0.5f - radius;
        float distance = Mathf.Sqrt(dx * dx + dy * dy);
        return new Color(1f, 1f, 1f, Mathf.Clamp01(radius - distance + 0.5f));
    }

    // ⚠ 모양: 흰색 삼각형 안에 느낌표를 투명하게 뚫는다. 색은 Image color(ErrorText)로 입힌다.
    private static Color WarningPixel(int x, int y, int width, int height)
    {
        float px = x + 0.5f;
        float py = y + 0.5f;

        // 삼각형 꼭짓점 (아래 왼쪽, 아래 오른쪽, 위 가운데)
        Vector2 a = new Vector2(1.5f, 3f);
        Vector2 b = new Vector2(width - 1.5f, 3f);
        Vector2 c = new Vector2(width * 0.5f, height - 2f);

        // 세 변까지의 부호 있는 거리 중 가장 작은 값 → 안쪽이면 양수
        float inside = Mathf.Min(EdgeDistance(a, b, px, py),
                       Mathf.Min(EdgeDistance(b, c, px, py), EdgeDistance(c, a, px, py)));
        float alpha = Mathf.Clamp01(inside + 0.5f);

        // 느낌표: 세로 막대 + 아래 점
        float center = width * 0.5f;
        bool inBar = Mathf.Abs(px - center) < 2f && py > 12f && py < 23f;
        bool inDot = Mathf.Abs(px - center) < 2f && py > 6.5f && py < 10f;
        if (inBar || inDot)
        {
            alpha = 0f;
        }

        return new Color(1f, 1f, 1f, alpha);
    }

    // 반시계 방향 변(from → to)의 왼쪽(삼각형 안쪽)이 양수인 거리
    private static float EdgeDistance(Vector2 from, Vector2 to, float px, float py)
    {
        Vector2 edge = to - from;
        Vector2 toPoint = new Vector2(px, py) - from;
        float cross = edge.x * toPoint.y - edge.y * toPoint.x;
        return cross / edge.magnitude;
    }
}
