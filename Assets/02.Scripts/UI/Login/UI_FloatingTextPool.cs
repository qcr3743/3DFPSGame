using System.Collections.Generic;
using TMPro;
using UnityEngine;

// "+1 KILL"처럼 지정 위치에서 위로 떠오르며 사라지는 텍스트. 연사해도 겹쳐 보이도록 몇 개를 풀로 돌려 쓴다.
// 이 오브젝트는 화면 전체를 채우고(앵커 stretch, 피벗 가운데), 템플릿 텍스트는 앵커·피벗이 가운데여야 한다.
public class UI_FloatingTextPool : MonoBehaviour
{
    [SerializeField] private TMP_Text _template;        // 비활성 템플릿 (복제해서 사용)
    [SerializeField] private int _poolSize = 8;
    [SerializeField] private string _text = "+1 KILL";
    [SerializeField] private float _riseDistance = 30f;
    [SerializeField] private float _duration = 0.6f;
    [SerializeField] private Vector2 _spawnOffset = new Vector2(0f, 24f);   // 히트마커와 겹치지 않게 살짝 위에서 시작

    private class Item
    {
        public TMP_Text Text;
        public RectTransform Rect;
        public Vector2 Start;
        public float Age;
        public bool IsAlive;
    }

    private readonly List<Item> _items = new List<Item>();
    private Color _baseColor;

    private void Awake()
    {
        if (_template == null)
        {
            return;
        }

        _baseColor = _template.color;
        _template.gameObject.SetActive(false);

        for (int i = 0; i < _poolSize; i++)
        {
            TMP_Text clone = Instantiate(_template, transform);
            clone.name = _template.name + "_" + i;
            clone.gameObject.SetActive(false);
            _items.Add(new Item { Text = clone, Rect = clone.rectTransform });
        }
    }

    private void OnDisable()
    {
        foreach (Item item in _items)
        {
            item.IsAlive = false;
            item.Text.gameObject.SetActive(false);
        }
    }

    public void Spawn(Vector2 localPosition)
    {
        Item item = GetFreeOrOldest();
        if (item == null)
        {
            return;
        }

        item.Start = localPosition + _spawnOffset;
        item.Age = 0f;
        item.IsAlive = true;
        item.Text.text = _text;
        item.Rect.anchoredPosition = item.Start;
        item.Text.color = _baseColor;
        item.Text.gameObject.SetActive(true);
        item.Rect.SetAsLastSibling();   // 새로 뜬 텍스트가 위에 보이게
    }

    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;

        foreach (Item item in _items)
        {
            if (!item.IsAlive)
            {
                continue;
            }

            item.Age += deltaTime;
            float t = _duration > 0f ? item.Age / _duration : 1f;
            if (t >= 1f)
            {
                item.IsAlive = false;
                item.Text.gameObject.SetActive(false);
                continue;
            }

            // 위치: 빠르게 올라가다 감속 / alpha: 처음엔 또렷하다가 끝으로 갈수록 빠르게 사라짐
            float rise = 1f - (1f - t) * (1f - t);
            item.Rect.anchoredPosition = item.Start + new Vector2(0f, _riseDistance * rise);

            Color color = _baseColor;
            color.a = _baseColor.a * (1f - t * t);
            item.Text.color = color;
        }
    }

    private Item GetFreeOrOldest()
    {
        Item oldest = null;
        foreach (Item item in _items)
        {
            if (!item.IsAlive)
            {
                return item;
            }

            if (oldest == null || item.Age > oldest.Age)
            {
                oldest = item;
            }
        }
        return oldest;
    }
}
