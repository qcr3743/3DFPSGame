using System.Collections;
using UnityEngine;

public class CoroutineExample : MonoBehaviour
{
    // 매 초다마 시작 -> 1초 -> 2초 -> 3초를 센 다음에 특정 로그를 출력하고 싶음
    // 유니티의 코루틴:
    // 비동기 실행과 제어를 효율적으로 제공하는 기능

    // 동기 vs 비동기
    // - 동기: 어떤 함수(작업)을 실행할 때 그 작업이 끝나기를 기다리는 방식 (이전 작업이 완료될 때까지 대기하고 다음 코드 실행)
    // - 비동기: 어떤 함수(작업)을 실행할 때 그 작업이 완료되지 안헏라고 다음 코드(함수)를 실행하는 방식 (이전 작업이 완료될 때까지 대기하고 다음 코드 실행)

    // 1. 비동기 실행
    // 2. 시간/프레임에 따른 실행 제어
    // - IEnumerator: 코루틴 함수의 반환형
    // - StartCoroutine(함수()): 유니티에서 코루틴 함수를 실행하는 방법
    // - yield: 정해진 시간/프레임동안 쉼

    private void Start()
    {
        Debug.Log("시작!");

        // Countdown 메서드를 비동기 방식으로 실행
        // 유니티에서는 비동기로 처리해야 하는 로직을 위해 코루틴을 제공 Co + Routine: 협력 동작
        // 멀티스레드 아님
        StartCoroutine(CountDown_Coroutine());

        // 코루틴은 비동기 방식으로 실행을 시키기 때문에 시작 다음엥 바로 끝 출력
        Debug.Log("끝!");
    }

    private IEnumerator CountDown_Coroutine()
    {
        yield return new WaitForSeconds(1f);
        Debug.Log("1초!");
        yield return new WaitForSeconds(1f);
        Debug.Log("2초!");
        yield return new WaitForSeconds(1f);
        Debug.Log("3초!");
        yield return new WaitForSeconds(1f);
        Debug.Log("발사!");
    }
}