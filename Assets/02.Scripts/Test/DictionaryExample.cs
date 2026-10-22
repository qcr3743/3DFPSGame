using System;
using System.Collections.Generic;
using UnityEngine;

public class DictionaryExample : MonoBehaviour
{
    // 배열과 List는 인덱스를 이용해 요소에 접근
    // But, 딕셔너리는 요소를 저장할 때 "키"와 "값"을 한 쌍으로 저장
    // HashTalbe(=Dictionary)은 키와 값이 쌍으로 이루어진 데이터를 관리(CRUDS)할 때 사용하는 자료구조

    private void Start()
    {
        Dictionary<string, int> students =
            new Dictionary<string, int>() { { "무규규", 22 }, { "뜌따", 21 }, { "우옹애", 20 } };

        students["뜌따"] = 23;
        students.Add("요이", 17);
        students.Remove("우옹애");
        bool hasKey = students.ContainsKey("요이");

        foreach (KeyValuePair<string, int> item in students)
        {
            Debug.Log($"{item.Key}: {item.Value}");
        }
    }
}