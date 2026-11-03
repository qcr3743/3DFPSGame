using System;
using System.Globalization;
using TMPro;
using UnityEngine;

// HUD 오른쪽 위의 "UTC HH:mm:ss" 텍스트를 실제 UTC 시각으로 갱신한다.
public class UI_ClockText : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;           // 비어 있으면 같은 오브젝트의 TMP_Text
    [SerializeField] private float _interval = 1f;     // 갱신 주기(초)
    [SerializeField] private string _prefix = "UTC ";

    private float _timer;

    private void Awake()
    {
        if (_text == null)
        {
            _text = GetComponent<TMP_Text>();
        }
    }

    private void OnEnable()
    {
        // 켜지는 순간 바로 한 번 갱신해서 빌더가 넣어 둔 임시 시각이 보이지 않게 한다.
        _timer = 0f;
        Refresh();
    }

    private void Update()
    {
        _timer += Time.unscaledDeltaTime;
        if (_timer < _interval)
        {
            return;
        }

        _timer = 0f;
        Refresh();
    }

    private void Refresh()
    {
        if (_text == null)
        {
            return;
        }

        // 시스템 로케일에 따라 구분자가 바뀌지 않도록 InvariantCulture로 포맷한다.
        _text.text = _prefix + DateTime.UtcNow.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
    }
}
