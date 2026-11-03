using UnityEngine;
using UnityEngine.UI;

// 타일링 안개 텍스처의 uvRect를 가로로 흘려서 안개가 천천히 흐르는 것처럼 보이게 한다.
// 안개 레이어(RawImage)마다 하나씩 붙이고 속도를 다르게 주면 깊이감이 생긴다.
// 텍스처의 Wrap Mode가 Repeat이어야 끊김 없이 반복된다.
public class UI_FogScroll : MonoBehaviour
{
    [SerializeField] private RawImage _rawImage;      // 비어 있으면 같은 오브젝트의 RawImage
    [SerializeField] private float _scrollSpeed = 0.01f;  // 초당 uv 이동량 (양수 = 왼쪽으로 흐름)
    [SerializeField] private float _tiling = 1f;          // 화면 폭에 텍스처가 몇 번 반복될지

    private float _offset;

    private void Awake()
    {
        if (_rawImage == null)
        {
            _rawImage = GetComponent<RawImage>();
        }

        // 레이어끼리 같은 무늬가 겹쳐 보이지 않게 시작 위치를 다르게 한다.
        _offset = Random.value;
    }

    private void Update()
    {
        if (_rawImage == null)
        {
            return;
        }

        // 값이 계속 커지지 않게 0~1 범위로 되돌린다 (Repeat 텍스처라 결과는 같음).
        _offset = Mathf.Repeat(_offset + _scrollSpeed * Time.unscaledDeltaTime, 1f);
        _rawImage.uvRect = new Rect(_offset, 0f, _tiling, 1f);
    }
}
