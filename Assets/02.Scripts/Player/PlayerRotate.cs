using UnityEngine;

public class PlayerRotate : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 200f;
    private float _accumulationX = 0f;

    private void Update()
    {
        if (GameManager.Instance.State != GameState.Playing)
        {
            return;
        }

        // 1. 마우스 입력을 받는다
        float mouseX = Input.GetAxis("Mouse X");
        // 2. 마우스 입력에 따라 회전할 방향을 구한다
        _accumulationX += mouseX * _rotationSpeed * Time.deltaTime;
        // 3. 누적한 회전값을 카메라에 적용한다
        //누적한 회전값을 카메라에 적용
        transform.eulerAngles = new Vector3(0, _accumulationX, 0f);
    }
}