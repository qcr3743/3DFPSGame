using UnityEngine;

// 역할: 마우스를 움직이면 카메라를 그 방향으로 회전
public class CameraRotate : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 200f;

    private float _accumulationX = 0f; // 좌우 회전 각도
    private float _accumulationY = 0f; // 위아래 회전 각도

    private void Update()
    {
        // 1. 마우스 입력을 받는다
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        GameLogger.Log($"mouseX: {mouseX}, mouseY: {mouseY}");

        // 2. 마우스 입력에 따라 회전할 방향을 구한다
        _accumulationX += mouseX * _rotationSpeed * Time.deltaTime;
        _accumulationY += -mouseY * _rotationSpeed * Time.deltaTime;
        // 3. 누적하기 전에 내 변수에서 먼저 -90 ~ 90도로 제한
        _accumulationY = Mathf.Clamp(_accumulationY, -90, 90);
        // 4. 누적한 회전값을 카메라에 적용한다
        //누적한 회전값을 카메라에 적용
        transform.eulerAngles = new Vector3(_accumulationY, _accumulationX, 0f);
    }
}