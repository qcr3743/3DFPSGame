# LoginScene UI 작업 지시서 (A안)

> Claude Code용 작업 지시서. 시안: 캔버스 "3D FPS 로그인 시안"의 A안(로그인 / 회원가입).
> 이번 작업은 **눈에 보이는 것만** 만든다. 로그인/회원가입 기능(계정 저장, 검증, 씬 이동)은 내일 한다.

## 0. 작업 범위와 규칙

- 작업 대상 씬: `Assets/01.Scenes/LoginScene.unity` **하나만**. 현재 Main Camera, Directional Light만 있는 빈 씬이다.
- `GameScene`, `CullingLab` 및 그 씬이 쓰는 프리팹·스크립트는 수정하지 않는다.
- CLAUDE.md 규칙대로 `.unity` / `.prefab` / `.meta` 파일은 직접 편집하지 않는다.
  → UI 계층은 **에디터 메뉴 스크립트**로 만든다: `Tools > Login > Build Login UI`.
  사용자가 LoginScene을 열고 메뉴를 실행한 뒤 Ctrl+S로 저장한다.
- 연출용 런타임 스크립트는 써도 된다. 단 **로그인 기능 로직은 넣지 않는다** (버튼 OnClick에 계정 처리 없음).
- 새 스크립트 위치
  - 에디터: `Assets/02.Scripts/Editor/LoginSceneBuilder.cs`
  - 런타임: `Assets/02.Scripts/UI/Login/` (`UI_` 접두사, 코루틴은 `_Coroutine` 접미사, 주석은 한국어)
- 생성 이미지(그라데이션 등): `Assets/04.Images/Login/`
- 에디터에서 실행해 보지 않았으면 "동작 확인"이라고 말하지 않는다.

## 1. 디자인 토큰

| 이름 | 값 | 용도 |
|---|---|---|
| Ground | `#070908` | 카메라 배경, 어둠 그라데이션 색 |
| Text Primary | `#F2F3EF` | 제목, 입력 글자 |
| Text Secondary | `#B4BCB5` | 라벨 |
| Text Muted | `#9AA39C` | HUD 텍스트, 보조 문구 |
| Text Dim | `#7C857E` | placeholder, 버전 표기 |
| Accent (감염 녹색) | `#9EE62E` | 포인트 1색: 주 버튼, 포커스, LIVE 점, 조준 모서리 |
| Accent Text | `#0B0D0C` | 녹색 버튼 위 글자 |
| Error | `#FF6A4D` (글자 `#FF8A70`) | 오류 테두리/문구 (내일 연결) |
| Line | 흰색 alpha 0.14 | 구분선 |
| Field BG | 흰색 alpha 0.04 | 입력칸 바탕 |
| Field Border | 흰색 alpha 0.18 (hover 0.32, focus = Accent) | 입력칸 테두리 |

- 모서리: 전부 **각진 형태(radius 0)**. 주 버튼만 왼쪽 아래·오른쪽 위 모서리를 18px 깎은 사각형(sprite로 생성).
- 글꼴 (Google Fonts, OFL 무료 라이선스) — `Assets/08.Fonts/Login/`에 사용자가 넣는다
  - 타이틀: **Chakra Petch Bold**
  - HUD/장식 텍스트(영문·숫자): **JetBrains Mono Regular**
  - 한글 본문: **IBM Plex Sans KR** Regular / Medium / SemiBold (TMP Dynamic 폰트 에셋)
  - 폰트 에셋이 아직 없으면 TMP 기본 폰트로 만들고, `LoginSceneBuilder` 상단에 폰트 경로 상수로 모아 둬서 나중에 한 번에 바꿀 수 있게 한다.

## 2. 씬 구성

```
LoginScene
├─ Main Camera            Clear: Solid Color #070908, Post Processing ON
├─ Global Volume          (3. 포스트 프로세싱)
├─ EventSystem
└─ Canvas_Login           Screen Space - Camera (Main Camera, Plane Distance 10)
   │                      Canvas Scaler: Scale With Screen Size 1920x1080, Match 0.5
   ├─ BG
   │  ├─ BG_Image         Image, 전체 Stretch. 임시로 비워 둠 (배경 이미지 확정 후 교체)
   │  │                   + UI_BackgroundDrift
   │  ├─ BG_LeftShade     가로 그라데이션 (아래 표)
   │  ├─ BG_Vignette      방사형 그라데이션 (가장자리 검정 alpha 0.7)
   │  └─ BG_Scanline      1x3 반복 텍스처 (흰색 alpha 0.03 1줄), Raycast Target OFF
   ├─ HUD_Top
   ├─ TargetBracket       좀비 위치 표시 (배경 확정 후 위치 조정)
   ├─ Panel_Login
   ├─ Panel_Signup        (비활성 상태로 시작)
   ├─ HUD_Bottom
   └─ FX_Overlay          전환용 검은 화면 / 신호 간섭 (CanvasGroup, 평소 alpha 0)
```

- **Screen Space - Camera**로 하는 이유: Overlay면 포스트 프로세싱(Bloom, Grain, 색수차)이 UI에 안 먹는다.
- 모든 장식 이미지·텍스트는 `Raycast Target` OFF (입력칸·버튼·토글만 ON).

### BG_LeftShade 그라데이션 (왼쪽 → 오른쪽, 색 #070908)

| 위치 | 0% | 30% | 52% | 70% | 100% |
|---|---|---|---|---|---|
| alpha | 0.94 | 0.82 | 0.25 | 0 | 0 |

배경 이미지 자체도 Image 색을 약간 어둡게(대략 `#9E9E9E`) 주고, 채도 낮추기는 Volume에서 한다.

## 3. 포스트 프로세싱 (Global Volume)

| 효과 | 값 |
|---|---|
| Color Adjustments | Saturation -50, Contrast +12, Post Exposure -0.6 |
| Bloom | Threshold 0.9, Intensity 0.6 (녹색 버튼·조명탄 번짐) |
| Vignette | Intensity 0.35, Smoothness 0.4 |
| Film Grain | Type Medium1, Intensity 0.25 |
| Chromatic Aberration | 0 (신호 간섭 때만 스크립트로 잠깐 올림) |

## 4. 레이아웃 (기준 해상도 1920x1080, 좌상단 기준 px)

### HUD_Top (왼쪽 120 / 오른쪽 120 / 위 44, 높이 24)
- 왼쪽: `●`(8px 원, Accent, `UI_Blink`로 깜빡임) · `LIVE`(Accent) · `SECTOR 07 // QUARANTINE ZONE`
- 오른쪽: `41.3111° N  69.2797° E` · `UTC 05:53:07`(`UI_ClockText`로 실제 UTC 시각 갱신)
- JetBrains Mono 13px, 자간 넓게(Character Spacing 12), 색 Text Muted
- 아래 y=84에 가로선 (Line 색, 두께 1)

### Panel_Login (x 120, y 200, 폭 480) — 위에서 아래로 Vertical Layout
| 요소 | 내용 | 스타일 |
|---|---|---|
| Tag | `// ZOMBIE OUTBREAK` | JetBrains Mono 14, Accent |
| Title | `3D FPS` | Chakra Petch Bold 112, Text Primary, 위 간격 10 |
| Subtitle | `SURVIVE THE QUARANTINE ZONE` | JetBrains Mono 13, Text Muted, 위 14 |
| Header | 왼쪽 `로그인`(24, SemiBold) / 오른쪽 `01 / SIGN IN`(Mono 12, Dim) | 위 72 |
| Divider | 선 | 위 14 |
| Label + Input 아이디 | 라벨 14 / 입력칸 높이 56, placeholder `아이디를 입력하세요` | 위 28, 라벨-입력 간격 8 |
| Label + Input 비밀번호 | placeholder `비밀번호를 입력하세요`, Content Type: Password | 간격 20 |
| Toggle | `아이디 저장` 체크박스 18px | 간격 20 |
| Button_Login | 높이 60, Accent 바탕, 깎인 모서리 sprite, 왼쪽 `로그인`(19, SemiBold, Accent Text) 오른쪽 → 화살표 | 위 28 |
| Footer | `계정이 없나요?` + 밑줄 링크 `회원가입`(Accent) | 위 26, 15px |

- 입력 글자 17px, 입력칸 좌우 padding 18.

### Panel_Signup (x 120, y 150, 폭 480)
- Tag 동일, Title `3D FPS` 64px로 작게
- Header `회원가입` / `02 / REGISTER`
- 아이디 입력칸(오른쪽에 테두리만 있는 `중복 확인` 버튼, 높이 56)
- 비밀번호 + 아래 안내 문구 자리 `[비밀번호 규칙 안내 문구]` (13px, 내용은 내일 정함)
- 비밀번호 확인 + 아래 **오류 문구 자리** (⚠ 아이콘 + `비밀번호가 일치하지 않습니다`, Error 색). 평소엔 비활성, 오류 표시 연결은 내일
- Button_Signup `가입하기` (Button_Login과 같은 스타일)
- Footer `이미 계정이 있나요?` + 링크 `로그인`

### TargetBracket (x 830, y 470, 380x220)
- 네 모서리에 22px L자 선(두께 2, Accent)
- 위쪽 -30에 태그 `HOSTILE ×3`(Accent 바탕, 어두운 글자) + `DIST 240m`
- 위치는 배경 이미지의 좀비 무리에 맞춰 나중에 옮긴다.

### HUD_Bottom (왼쪽 120 / 오른쪽 120 / 아래 44)
- 왼쪽 `v0.1 · BUILD 2026.11`
- 오른쪽 `SERVER ● ONLINE` + `게임 종료` 버튼(테두리만, 높이 44)
- Mono 12, Text Dim

## 5. 연출 스크립트 (로그인 기능과 무관한 것만)

| 스크립트 | 붙는 곳 | 하는 일 |
|---|---|---|
| `UI_ButtonFeel` | 모든 버튼 | Hover: 0.15초 동안 살짝 밝아짐 + 위로 1px. Press: 아래로 1px + 어두워짐. Hover/Click 효과음 슬롯(AudioClip, 비어 있으면 무시). `IPointerEnter/Exit/Down/Up` 사용 |
| `UI_InputFieldFeel` | 입력칸 | 테두리 색: 기본 → hover → focus(Accent), 바탕도 focus 때 Accent alpha 0.06. 포커스 시 짧은 틱 효과음 슬롯 |
| `UI_PanelSwitcher` | Canvas_Login | `ShowLogin()` / `ShowSignup()` 공개 메서드. 전환 시 0.12초 신호 간섭(FX_Overlay 깜빡임 + 패널 x 흔들림 + Volume 색수차 잠깐 0.6) 후 패널 교체, 새 패널은 0.25초 페이드 + 왼쪽에서 24px 슬라이드 인. 회원가입/로그인 **링크 버튼의 OnClick만** 여기에 연결 |
| `UI_IntroSequence` | Canvas_Login | 씬 시작 시: 검은 화면 → 가운데 `CONNECTING...`(점 깜빡임, 1초) → 검은 화면 페이드 아웃 → HUD 선이 0→전체 폭으로 그어짐 → Panel_Login 페이드·슬라이드 인. 아무 키나 누르면 스킵 |
| `UI_BackgroundDrift` | BG_Image | 30초 동안 Scale 1.0→1.05, 왕복(SmoothStep) |
| `UI_Blink` | LIVE 점, SERVER 점 | alpha 1↔0.3 반복 (주기 Inspector) |
| `UI_ClockText` | UTC 시각 텍스트 | 1초마다 `UTC HH:mm:ss` 갱신 |

- 연출 시간 값은 전부 `[SerializeField] private`로 노출한다.
- `Time.unscaledDeltaTime` 기준 (나중에 일시정지와 무관하게).
- `GameManager` 상태 게이팅 대상 아님 (LoginScene엔 GameManager 없음). GameManager를 참조하지 않는다.
- `Button_Login`, `Button_Signup`, `중복 확인`, `게임 종료`의 OnClick은 **비워 둔다** (내일 연결).

## 6. LoginSceneBuilder (에디터 메뉴) 요구사항

- 메뉴: `Tools/Login/Build Login UI`
- 현재 활성 씬 이름이 `LoginScene`이 아니면 경고 다이얼로그 후 중단.
- 이미 `Canvas_Login`이 있으면 "지우고 다시 만들까요?" 확인 후 진행 (Undo 등록).
- 그라데이션/비네트/스캔라인/깎인 모서리 버튼 sprite는 코드로 Texture2D를 만들어 `Assets/04.Images/Login/`에 PNG로 저장하고 Sprite로 import 설정. 이미 있으면 재사용.
- 기존 Main Camera는 재사용해서 배경색·Post Processing만 설정. Directional Light는 그대로 둠.
- Global Volume과 Volume Profile(`Assets/Settings/Login/LoginVolumeProfile.asset`) 생성.
- 위 5번 스크립트를 해당 오브젝트에 붙이고 서로 참조 연결까지 한다.
- 끝나면 Console에 "LoginScene UI 생성 완료 — Ctrl+S로 씬 저장" 로그.

## 7. 사용자가 직접 할 것 (작업 후 안내)

1. Google Fonts에서 Chakra Petch, JetBrains Mono, IBM Plex Sans KR 내려받아 `Assets/08.Fonts/Login/`에 넣기
2. `Window > TextMeshPro > Font Asset Creator`로 TMP 폰트 에셋 만들기 (IBM Plex Sans KR은 Atlas Population Mode: Dynamic)
3. LoginScene 열고 `Tools > Login > Build Login UI` 실행 → Ctrl+S
4. 배경 이미지(또는 영상)가 확정되면 BG_Image에 넣고 TargetBracket 위치 조정
5. 효과음은 `Assets/06.Sounds/UI/`에 넣고 UI_ButtonFeel / UI_InputFieldFeel 슬롯에 연결

## 8. 나중 단계 (이번 작업에서 하지 않음)

- 배경 영상: VideoPlayer (Render Mode: Camera Far Plane, Loop, 음소거)
- 전장 연출: 랜덤 폭발 섬광 + 화면 흔들림 + 지연 효과음, 서치라이트, 조명탄 빛 번짐 스프라이트, 안개/재 파티클
- 로그인/회원가입 기능, 오류 시 패널 흔들림, 로그인 성공 → 무전 잡음 + 암전 → GameScene 이동
