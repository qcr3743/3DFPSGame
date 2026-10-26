/ㅡㅐ# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

Unity 6 (6000.3.22f1) + URP 17.3 기반의 3D FPS 학습용 게임 프로젝트이다. 메인 씬은 `Assets/01.Scenes/GameScene.unity`이다.

- 빌드·실행·테스트는 Unity Editor에서 한다. CLI 빌드 스크립트, 린터, 자동화 테스트(Test Framework 패키지는 있으나 테스트 코드 없음)는 없다.
- 코드 변경은 Unity Editor에서 컴파일되어야 확인되므로, 에디터에서 실행해 보지 않았다면 동작을 확인했다고 말하지 않는다.
- `.unity`, `.prefab`, `.meta` 같은 직렬화 파일은 직접 수정하지 않는다. 씬/프리팹에서 필요한 설정(Inspector 참조 연결 등)은 사용자에게 안내한다.
- 답변은 한국어로 작성한다.

## 코드 리뷰

코드 리뷰를 요청받으면 `Docs/CodeReviewGuide.md`의 기준(중요도 분류, 리뷰 결과 형식 포함)을 그대로 따른다. 요청받지 않았다면 리뷰 중에 코드를 직접 변경하거나 커밋하지 않는다.

## 아키텍처

스크립트는 `Assets/02.Scripts/` 아래 기능별 폴더(Camera, Core, Game, Monster, Player, UI, Weapon, Environment)로 나뉜다. `Test/` 폴더는 C# 문법 학습용 예제(List, Dictionary, Generic, Coroutine)이며 게임 로직과 무관하다.

### 게임 상태 게이팅
`GameManager`(싱글톤, `GameManager.Instance`)가 `GameState`(Ready → Playing → GameOver)를 관리한다. 시작 시 코루틴으로 "준비/시작" 연출 후 `Playing`으로 전환된다. 플레이어 입력, 좀비 AI, 커서 제어 등 대부분의 `Update()`는 첫 줄에서 `State != GameState.Playing`이면 return한다. 새 게임플레이 컴포넌트도 이 패턴을 따른다.

### 데미지 시스템
`Core/IDamageable` 인터페이스와 `Core/DamageInfo` 구조체(데미지 양, 피격 위치, 피격 노멀)로 통일되어 있다. `Player`, `Zombie`, `Box`, `Drum` 등 맞을 수 있는 대상은 모두 `IDamageable.TakeDamage(DamageInfo)`를 구현하고, 총/폭탄은 구체 타입이 아닌 `IDamageable`로 데미지를 전달한다. 사망 판단·후처리는 각 객체의 `TakeDamage` 내부에서 처리한다(플레이어 사망 시 `GameManager.Instance.GameOver()` 호출).

### 몬스터 AI
`Monster/Zombie`는 `ZombieState` enum 기반 FSM(Idle, Trace, Comeback, Attack, Hit, Death)이다. `Update()`의 switch로 상태별 메서드를 호출하고, Hit/Death는 코루틴으로 처리한다. 이동은 `CharacterController`, 플레이어는 `"Player"` 태그로 찾는다. `ZombieHealthBar`는 빌보드 체력바이다(체력바 자신만 회전시켜야 함).

### 플레이어
역할별 컴포넌트로 분리되어 있다: `Player`(체력/피격), `PlayerMove`, `PlayerRotate`, `PlayerGunFire`(레이캐스트 사격·재장전 코루틴), `PlayerBombFire`(폭탄 투척). 폭탄은 `Weapon/BombPool`(Queue 기반 오브젝트 풀)에서 `Get`/`Return`으로 재사용한다.

### 입력과 커서
입력은 레거시 `UnityEngine.Input` API(`Input.GetButton`, `Input.GetKeyDown`)를 사용한다. Input System 패키지와 `InputSystem_Actions.inputactions`가 있지만 스크립트에서는 쓰지 않는다. `CursorManager`가 커서를 잠그고, LeftAlt를 누르는 동안 커서를 표시한다. `PlayerGunFire`는 `Cursor.visible`이 true면 발사하지 않는다(UI 클릭 시 오발사 방지).

### 미니맵
`Camera/MinimapCamera`가 플레이어 머리 위에서 내려다보며 따라가고(LateUpdate), RenderTexture를 마스크로 둥글게 잘라 UI에 표시한다. `ZoomIn`/`ZoomOut`은 orthographicSize를 조절하며, `UI/UI_Minimap`에서 +/- 버튼과 연결하는 작업이 진행 중이다.

## 코드 컨벤션

- private 필드는 `_camelCase`, Inspector 노출은 `[SerializeField] private`, 외부 읽기는 `=>` 읽기 전용 프로퍼티로 노출한다.
- UI 스크립트는 `UI_` 접두사, 코루틴 메서드는 `_Coroutine` 접미사를 붙인다(예: `Reloading_Coroutine`).
- 주석은 한국어로, 학습용 프로젝트라 동작 의도를 설명하는 주석이 비교적 많다. 주변 코드의 주석 밀도를 따른다.
- 커밋 메시지는 `feat: ...`, `fix: ...` 형식에 한국어 설명을 사용한다.
