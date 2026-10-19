using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 7f;
    private CharacterController _characterController;

    private void Awake()
    {
        _characterController = gameObject.GetComponent<CharacterController>();
    }

    private void Update()
    {
        // 1. 키보드 입력을 받는다
        float h = Input.GetAxisRaw("Horizontal"); // -1, 0, 1만 반환
        float v = Input.GetAxisRaw("Vertical"); // -1, 0, 1만 반환

        // 2. 키보드 입력에 따라 방향을 구한다
        Vector3 direction = new Vector3(h, 0, v);
        direction.Normalize(); //대각선이어도 방향의 길이를 1로 고정하기 위해서
        direction = transform.TransformDirection(direction);
        // 로컬 좌표계: 한 대상을 중심
        // 월드 좌표계: 세상을 중심

        //3. 방향과 속력에 따라 이동한다
        _characterController.Move(direction * _moveSpeed * Time.deltaTime);
    }
}