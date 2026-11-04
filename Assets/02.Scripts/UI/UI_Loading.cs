using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 로딩 씬 UI. SceneLoader.Load()로 넘겨받은 다음 씬을 비동기로 불러오면서
// 진행 정도를 로딩바(슬라이더)와 "불러오는 중 N%" 텍스트로 보여준다.
public class UI_Loading : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Slider _loadingSlider;
    [SerializeField] private TMP_Text _loadingText;

    [Header("연출")]
    [SerializeField] private float _fillSpeed = 1f;   // 로딩바가 1초에 차오르는 양 (1이면 빈 바가 다 차는 데 최소 1초)

    private const string LoadingTextFormat = "불러오는 중 {0}%";

    private void Start()
    {
        StartCoroutine(Load_Coroutine());
    }

    private IEnumerator Load_Coroutine()
    {
        // 로그인 씬을 거치지 않고 로딩 씬부터 실행하면 다음 씬 이름이 비어 있다.
        if (string.IsNullOrEmpty(SceneLoader.NextSceneName))
        {
            Debug.LogError("[UI_Loading] 불러올 씬 이름이 없습니다. SceneLoader.Load()로 로딩 씬에 들어와야 합니다.");
            yield break;
        }

        // 비동기 씬 로드!
        // 반환: 씬의 비동기 진행사항을 확인할 수 있는 객체
        AsyncOperation operation = SceneManager.LoadSceneAsync(SceneLoader.NextSceneName);
        // 다 불러와도 바로 넘어가지 않고 0.9에서 기다리게 한다. (로딩바가 끝까지 찬 뒤에 넘기기 위해)
        operation.allowSceneActivation = false;

        float displayValue = 0f;   // 화면에 보여주는 진행률 (0~1)
        SetProgress(displayValue);

        while (true)
        {
            // progress는 0~0.9까지가 불러오기, 0.9에서 활성화를 기다린다. → 0~1로 바꿔서 쓴다.
            float targetValue = Mathf.Clamp01(operation.progress / 0.9f);

            // 실제 로딩이 한 프레임에 끝나도(에디터에서 흔함) 바가 튀지 않게 목표값을 천천히 따라간다.
            displayValue = Mathf.MoveTowards(displayValue, targetValue, _fillSpeed * Time.unscaledDeltaTime);
            SetProgress(displayValue);

            // 불러오기가 끝났고 로딩바도 끝까지 찼으면 다음 씬으로 넘어간다.
            if (operation.progress >= 0.9f && displayValue >= 1f)
            {
                operation.allowSceneActivation = true;
                yield break;
            }

            yield return null;
        }
    }

    private void SetProgress(float value)
    {
        _loadingSlider.value = value;
        _loadingText.text = string.Format(LoadingTextFormat, Mathf.RoundToInt(value * 100f));
    }
}
