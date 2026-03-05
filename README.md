# Apex Follower

Le Mans Ultimate(rFactor2) 전용 **실시간 드라이빙 가이드 오버레이**.  
고수의 주행 데이터(레퍼런스 랩)를 기반으로, 게임 위에 투명하게 떠서 브레이크·스로틀·스티어링·기어를 실시간으로 안내합니다.

## 핵심 기능 (4가지)

| 기능 | 설명 |
|------|------|
| **Ghost Pedals** | 고수의 브레이크/스로틀 위에 내 실시간 입력을 채워진 막대로 겹쳐 표시 |
| **Target Gear & RPM** | 레퍼런스 기어와 다를 때 화면 중앙에 변속 알림 + 펄스 애니메이션 |
| **Ghost Steering** | 화면 하단 중앙에 레퍼런스(점선 호)와 내 조향(실선 호)을 겹쳐 표시 |
| **Delta Distance** | 같은 LapDistance에서 레퍼런스 대비 시간차를 화면 상단에 표시 |

### 색상 체계 (3색 Delta 시스템)

- **Green** — 레퍼런스와 일치 (차이 5% 미만)
- **Blue** — 입력이 레퍼런스보다 부족 (더 밟거나 꺾어야 함)
- **Red** — 입력이 레퍼런스를 초과 (오버스티어 / 휠락 위험)

모든 컴포넌트(Ghost Pedals, Ghost Steering, Target Gear)에 동일하게 적용됩니다.

---

## 동작 원리

```
LMU 게임
  └─ Shared Memory ($rFactor2SMMP_Telemetry$, $rFactor2SMMP_Scoring$)
       └─ SharedMemoryReader (백그라운드 100Hz 폴링)
            └─ TelemetrySnapshot (경량 9필드 구조체)
                 ├─ LapDetector → 랩 완료 시 레퍼런스 자동 저장
                 ├─ ReferenceLapStore → %APPDATA%/ApexFollower/ReferenceLaps/*.json
                 └─ DistanceMatchEngine (이진 탐색 + 선형 보간)
                      └─ OverlayWindow (투명·클릭투과 WPF 창, vsync 렌더링)
                           ├─ PedalBar (Brake / Throttle)
                           ├─ TargetGear
                           ├─ GhostSteering
                           └─ DeltaDistance
```

### 선형 보간 공식

레퍼런스 데이터 포인트 사이를 매끄럽게 보간합니다:

```
V = V1 + (V2 - V1) * (Pos_current - Pos1) / (Pos2 - Pos1)
```

---

## 프로젝트 구조

```
LeMansUltimateCoPilot/
├── SharedMemory/
│     ├── Rf2Structs.cs            rF2 공식 메모리 구조체
│     ├── SharedMemoryReader.cs    100Hz 폴링, 트랙/차량 변경 감지
│     └── TelemetrySnapshot.cs    경량 9-필드 readonly struct
├── Data/
│     ├── ReferenceLapStore.cs     JSON 저장/로드 (%APPDATA%/ApexFollower/)
│     └── DistanceMatchEngine.cs  O(log n) 이진 탐색 + 선형 보간
├── Services/
│     └── LapDetector.cs          랩 완료 감지 및 이벤트 발생
├── Overlay/
│     ├── OverlayWindow.xaml/.cs  투명 클릭투과 메인 윈도우
│     ├── PedalBar.xaml/.cs       브레이크 / 스로틀 Progressive Bar
│     ├── TargetGear.xaml/.cs     기어 표시 + ScaleTransform 애니메이션
│     ├── GhostSteering.xaml/.cs  Path + ArcSegment 스티어링 가이드
│     └── DeltaDistance.xaml/.cs  타임 델타 표시
├── App.xaml/.cs                  WPF 진입점, 자동 레퍼런스 로딩
└── LeMansUltimateCoPilot.csproj
```

---

## 요구 사항

- **Windows 10/11** (Shared Memory는 Windows 전용)
- **.NET 10.0** 이상 (Windows 대상 빌드)
- **Le Mans Ultimate** 실행 중 (활성 드라이빙 세션)
- **rFactor2SharedMemoryPlugin** 로드됨 (LMU 기본 포함)

---

## 빌드 및 실행

```powershell
# 빌드
dotnet build -c Release

# 실행
dotnet run
# 또는 bin/Release/net10.0-windows/ApexFollower.exe 직접 실행
```

Visual Studio에서는 솔루션 파일(`LeMansUltimateCoPilot.sln`)을 열고 F5로 실행합니다.

---

## 사용법

1. **Le Mans Ultimate를 실행**하고 드라이빙 세션에 진입합니다.
2. **ApexFollower를 실행**합니다. 오버레이가 게임 위에 자동으로 고정됩니다.
3. **첫 랩을 주행**합니다. 랩이 완료되면 자동으로 레퍼런스 랩이 저장됩니다.
4. **이후 랩부터** 오버레이가 활성화되어 레퍼런스와 비교 가이드를 제공합니다.
5. 더 빠른 랩을 완료하면 **자동으로 베스트 랩으로 갱신**됩니다.

### 레퍼런스 랩 저장 위치

```
%APPDATA%\ApexFollower\ReferenceLaps\
  {TrackName}_{VehicleName}_{LapTime}s.json
```

트랙/차량이 바뀌면 해당 트랙의 베스트 레퍼런스가 자동으로 불러와집니다.

---

## 오버레이 HUD 레이아웃

```
 ┌────────────────────────────────────────────┐
 │           ┌─────────────┐                  │
 │           │  -0.342s    │  ← Delta         │
 │           └─────────────┘                  │
 │                                            │
 │ ▌BRAKE▐          ▌3▐         ▌THROTTLE▐   │
 │ ▌█████▐          ▌ ▐         ▌█████████▐  │
 │ ▌██▁▁▁▐  (gear)  ▌ ▐         ▌▁▁▁██████▐  │
 │ ▌▁▁▁▁▁▐          ▌ ▐         ▌▁▁▁▁▁▁▁▁▁▐  │
 │                                            │
 │           ╭───── ◎ ─────╮                  │
 │           ╰─────────────╯  ← Steering      │
 └────────────────────────────────────────────┘

  채워진 막대 = 내 실시간 입력
  수평선 마커 = 레퍼런스(고수) 기준값
  색상        = Green(일치) / Blue(부족) / Red(초과)
```

---

## 안티치트 안전성

모든 데이터 읽기는 **rFactor2 공식 Shared Memory API**를 통한 External 방식입니다.  
게임 프로세스에 대한 Injection/Hooking을 일절 사용하지 않으므로 온라인 레이스에서도 안전합니다.

---

## 기술 스택

| 항목 | 내용 |
|------|------|
| 프레임워크 | .NET 10.0 WPF (Windows) |
| 공유 메모리 | `System.IO.MemoryMappedFiles` |
| Win32 인터롭 | `User32.dll` SetWindowLong (클릭 투과) |
| 렌더링 | `CompositionTarget.Rendering` (vsync 동기화) |
| 데이터 폴링 | 100Hz (10ms 주기, 백그라운드 스레드) |
| 직렬화 | `System.Text.Json` |
| 외부 NuGet | 없음 |

---

## 문제 해결

### "Waiting for LMU..." 메시지가 계속 표시될 때
- Le Mans Ultimate가 실행 중이고 드라이빙 세션에 진입했는지 확인합니다.
- 메인 메뉴 상태에서는 Shared Memory가 생성되지 않습니다.

### 오버레이가 게임 위에 표시되지 않을 때
- 게임이 **전체 창(Borderless Windowed)** 모드인지 확인합니다.
- 전체 화면(Exclusive Fullscreen) 모드에서는 오버레이가 가려질 수 있습니다.

### 공유 메모리 접근 오류
- 관리자 권한으로 실행해 보세요.
- 안티바이러스가 Shared Memory 접근을 차단하지 않는지 확인합니다.

---

## 라이선스

개인 학습 및 비상업적 사용 목적. Le Mans Ultimate 및 rFactor2는 각 소유권자의 상표입니다.
