using UnityEngine;

public class UI_Minimap : MonoBehaviour
{
    [SerializeField] private Camera _minimapCamera;
    [SerializeField] private float _zoomStep = 1f;
    [SerializeField] private float _minZoom = 3f;
    [SerializeField] private float _maxZoom = 15f;

    // +버튼 OnClick에 연결: 확대 = orthographicSize 감소
    public void ZoomIn()
    {
        SetZoom(_minimapCamera.orthographicSize - _zoomStep);
    }

    // -버튼 OnClick에 연결: 축소 = orthographicSize 증가
    public void ZoomOut()
    {
        SetZoom(_minimapCamera.orthographicSize + _zoomStep);
    }

    private void SetZoom(float size)
    {
        _minimapCamera.orthographicSize = Mathf.Clamp(size, _minZoom, _maxZoom);
    }
}
