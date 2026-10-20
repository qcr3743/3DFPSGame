using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 7f;
    private const float Gravity = -20f;
    private float _yVelocity = 0f; // 위 아래 속도, 매 프레임마다 중력 가속도가 붙음
    private CharacterController _characterController;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
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
        // 캐릭터의 회전 방향을 기준으로 로컬 방향 벡터를 월드 방향 벡터로 변환
        // 로컬 좌표계: 한 대상을 중심
        // 월드 좌표계: 세상을 중심

        // 3. 중력을 누적한다
        _yVelocity += Gravity * Time.deltaTime;

        // 4. 속도를 구한다
        Vector3 velocity = direction * _moveSpeed;
        velocity.y = _yVelocity;

        // 5. 속도(방향과 속력)에 따라 이동한다
        _characterController.Move(velocity * Time.deltaTime);
    }
}