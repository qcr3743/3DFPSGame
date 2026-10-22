using UnityEngine;
using System.Collections.Generic;

public class ListExample : MonoBehaviour
{
    // 자료 구조(Data Structure)
    // 이런 데이터들을 "효율적으로" 관리(CRUDS)하기 위해 저장하는 구조 방식
    // C - Creat:  생성
    // R - Read:   읽기
    // U - Update: 수정
    // D - Delete: 삭제
    // S - Search: 조회
    // Bread

    // 선형 자료구조  : 배열, 리스트, 큐, 스택
    // 비선형 자료구조: 트리, 그패트, 해쉬테이블

    private void Start()
    {
        int[] ages = new int[] { 22, 21, 20, 35 }; // 배열의 선언과 정의

        // 1. 배열은 선언할 때 지정한 배열의 크기를 변경할 수 없음
        // -> 배열에 데이터를 추가하거나 삭제하려면 새로운 배열을 생성해야함
        // -> 배열은 데이터를 중간에 추가하거나 삭제하는 경우 비효율적인 과정이 필요
        // ages[4] = 30; // -> 범위를 벗어났기 때문에 IndexOutOfRange 에러가 뜸

        // 배열의 단점 보완 -> 동적 배열 혹은 연결 리스트를 사용
        // C#은 이것을 "콜렉션" 네임스페이스 안에 List로 구현함
        // 콜렉션(Collection): 컴퓨터공학의 자료구조를 C#에서 구조화하여 클래스로 구현해 놓은 것들

        // List -> 배열과 똑같은데 필요에 따라 크기가 동적으로 증감하는 배열
        List<int> ages2 = new List<int>() { 22, 21, 20, 35 };
        ages2.Add(24); // 데이터를 끝에 추가
        ages2.Insert(1, 12); // 데이터를 중간에 추가
        ages2.Remove(22);
        ages2.RemoveAt(0);
        foreach (int age in ages2)
        {
            Debug.Log(age);
        }
    }
};