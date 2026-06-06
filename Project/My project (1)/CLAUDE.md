# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

Meta Quest 3 대상 피험자 간 설계(between-subjects) VR 실험 프로젝트. Unity 6 + URP 기반.
단계적 시각 정보 노출이 VR 멀미 감소 및 조준 수행 능력에 미치는 영향을 측정한다.

- **1군(대조군)**: 처음부터 완전한 시각 → Moving Phase(2분) → Static Phase(전체 처치)
- **2군(실험군)**: Phase1 암흑(NavMission) → Phase2 엣지(NavMission) → Moving Phase(2분) → Static Phase(전체 처치)

## 에디터 자동화 (VR Adaptation 메뉴)

씬 설정은 아래 메뉴 항목으로 처리한다. Inspector 수동 연결보다 우선한다.

| 메뉴 항목 | 용도 |
|----------|------|
| `VR Adaptation → Auto Setup Adaptation System` | `VR_Adaptation_Manager`, `Aim_Trainer_Manager`, `BlackoutCanvas`, `AimTrainerHUD` 생성 및 Right Controller에 `RaycastWeapon`+`VRGunController` 부착, XR Origin에 `ProximityFeedback`+`CharacterController` 부착 |
| `VR Adaptation → Setup Wall Collision (CharacterController)` | XR Origin에 `CharacterController`(h=1.8, r=0.3) + `VRCharacterCollision` 추가. 엄지 스틱 이동 시 벽 통과 차단 |
| `VR Adaptation → Setup Phase Outline Feature (DepthNormals)` | 프로젝트 내 모든 `UniversalRendererData`에 `PhaseOutlineFeature` 추가 |
| `VR Adaptation → Add XR Device Simulator (Mouse Test)` | PC 키보드/마우스 VR 시뮬레이터 추가. **기기 빌드 전 반드시 제거** |

## PC 테스트 조작법 (XR Device Simulator)

| 입력 | 동작 |
|------|------|
| 마우스 우클릭 드래그 | HMD 시선 회전 |
| WASD / 화살표 키 | 이동 |
| Left Shift + Space | 오른쪽 컨트롤러 모드 전환 |
| 좌클릭 | 활성 컨트롤러 트리거 (발사) |

**타겟을 맞히려면**: Shift+Space로 오른쪽 컨트롤러 모드 진입 → 마우스로 조준 → 좌클릭 발사.

## 디버그 옵션 (VRAdaptationManager Inspector)

- `m_DebugFastMode = true` — Moving Phase 시간을 1/10으로 단축 (빠른 테스트용)
- `m_ForceControlGroup = true` — 적응 Phase를 건너뛰고 Moving Phase로 직행

## 아키텍처

### 실험 흐름

```
LobbyScene  →  ExperimentCondition (static, 씬 간 유지)
                ├─ SelectedGroup: Control | Adaptation
                └─ ParticipantID

BasicScene  →  VRAdaptationManager.Start()
                ├─ 1군(대조군): ControlSequence()
                │     └─ Moving Phase(2분) → Static Phase(전체 Enemy 처치) → Complete
                │
                └─ 2군(실험군): AdaptationSequence()
                      ├─ Phase1_Blackout  : 완전 암흑 + NavigationMission (체크포인트 도달 시 종료)
                      ├─ Phase2_Edge      : 엣지/실루엣 + NavigationMission (체크포인트 도달 시 종료)
                      ├─ Moving Phase(2분): 이동식 Enemy 타겟 처리
                      └─ Static Phase     : 씬 배치 Enemy 전체 처치 → Complete
```

각 Phase 전환 시 `ResetPlayerToSpawn()`으로 XR Origin을 `m_PlayerSpawnPoint`로 순간이동.

### AdaptationPhase 열거형

```csharp
None
Phase1_Blackout      // 2군 전용: 암흑 + NavMission
Phase2_Edge          // 2군 전용: 엣지/실루엣 + NavMission
AimTrainer_Moving    // 양군 공통: 이동 타겟 (2분)
AimTrainer_Static    // 양군 공통: 정적 타겟 전체 처치 (무제한)
Complete
```

### 네비게이션 미션 시스템 (2군 Phase 1/2 전용)

Phase 1/2는 시간 고정이 아닌 **체크포인트 도달** 기반으로 종료된다.

- `NavigationMission` — Zone 목록을 순서대로 활성화, 마지막 Zone 진입 시 `OnCompleted` 발생
- `PhaseCheckpointZone` — Trigger Collider 기반 체크포인트. `m_LinkedGlows`에 `GlowGuidePoint` 연결 가능
- `GlowGuidePoint` — 발광 안내 구체 (Unlit, 펄스 애니메이션). `GlobalAdaptationEffect` 셰이더 교체에서 제외됨

Inspector 연결 필요:
- `VRAdaptationManager.m_Phase1NavMission` → Phase 1용 NavigationMission 오브젝트
- `VRAdaptationManager.m_Phase2NavMission` → Phase 2용 NavigationMission 오브젝트
- `VRAdaptationManager.m_PlayerSpawnPoint` → Phase 전환 시 리셋 위치

### AimTrainer 구조 (COD 스타일)

**Moving Phase** (`AimTrainer_Moving`)
- 이동식 Enemy 타겟이 Patrol 경로로 순찰
- `AimTargetManager.StartMovingPhase()` 호출, 2분 시간 제한
- `AimTrainerHUD`에 타이머 표시

**Static Phase** (`AimTrainer_Static`)
- 씬에 배치된 Enemy 타겟 전체 처치 시 자동 종료 (시간 제한 없음)
- `AimTargetManager.OnAllEnemiesKilled` 콜백으로 완료 감지
- `AimTrainerHUD`에 남은 적 카운터 표시

**AimTarget 타입**
- `Enemy` — 정상 타겟. 처치 시 카운트 차감
- `Friendly` — 오발 시 FriendlyFire 로그 및 패널티

`RaycastWeapon` 레이저 조준선은 기본 **OFF** (가늠좌 조준). Inspector `m_ShowLaserSight`로 ON/OFF.

### 시각 피델리티 파이프라인

`VRAdaptationManager`가 글로벌 셰이더 float `_GlobalVisualFidelity` (0→1)를 구동한다.

| Phase | fidelity | 시각 상태 |
|-------|----------|----------|
| Phase1_Blackout | 0.0 | 완전 암흑 (카메라 배경 SolidColor/Black, 조명 OFF) |
| Phase2_Edge | 0.3 (고정) | 검정 배경 + 파란 윤곽선 (Inverted Hull) |
| AimTrainer_Moving/Static | 1.0 | 완전 렌더링 (원본 머티리얼 복원) |

1. **`GlobalAdaptationEffect`** — Awake 시 씬의 모든 `Renderer` 머티리얼을 `VR/AdaptationProgressive` (`Assets/Shaders/ProgressiveLOD.shader`)로 교체. 오브젝트명에 `vignette`, `sky`, `ui`, `glow` 포함 또는 Layer 5(UI)인 경우 제외.
2. **윤곽선 (Phase 2)** — `ProgressiveLOD.shader`의 Outline Pass (Inverted Hull, back-face 팽창 방식). `step(0.29, fidelity)`로 Phase 2 진입 즉시 활성화, fidelity 0.62~0.72에서 페이드아웃.
3. **Phase 1 완전 암흑** — 카메라 배경 `SolidColor/Black` 전환 + 씬 조명/환경광 OFF.
4. **접촉 Glow 링** — `SurfaceGlowManager`가 컨트롤러 벽 접촉을 감지해 파란 링을 셰이더 전역으로 주입. Phase 3(fidelity≥0.7)에서 즉시 클리어.

`VRAdaptationManager`는 반드시 `GlobalAdaptationEffect`보다 먼저 실행 — `[DefaultExecutionOrder(-100)]`으로 보장.

### 주요 싱글톤 및 클래스

| 클래스 | 게임오브젝트 | 역할 |
|--------|------------|------|
| `VRAdaptationManager` | `VR_Adaptation_Manager` | Phase 시퀀스 제어, 피델리티 갱신, 이벤트 버스(`OnPhaseChanged`) |
| `GlobalAdaptationEffect` | `VR_Adaptation_Manager` | 셰이더 교체 / 원본 복원 |
| `NavigationMission` | 씬 배치 오브젝트 | Phase 1/2 체크포인트 미션 관리 |
| `PhaseCheckpointZone` | 씬 배치 오브젝트 | Trigger 체크포인트, GlowGuidePoint 연결 |
| `GlowGuidePoint` | 씬 배치 오브젝트 (프리팹) | 발광 안내 구체, 펄스 애니메이션 |
| `SurfaceGlowManager` | `VR_Adaptation_Manager` | 벽 접촉 Glow 링 감지 및 셰이더 주입 |
| `AimTargetManager` | `Aim_Trainer_Manager` | Moving/Static Phase 타겟 관리, `OnAllEnemiesKilled` 콜백 |
| `ExperimentDataLogger` | `Aim_Trainer_Manager` | CSV 로깅 |
| `AimTrainerHUD` | `AimTrainerHUD` | World Space 타이머/카운터 UI |

### 발사 시스템

`RaycastWeapon` + `VRGunController`가 Right Hand Controller에 부착된다. 발사 액션: `XRI Right Hand Interaction/Activate`. `m_FireClip`, `m_HitClip`은 `Assets/Sounds/`에서 Inspector 할당. `m_TargetLayer`는 AimTarget 프리팹 레이어 포함 필요.

### 실험 데이터 저장 위치

`ExperimentDataLogger`가 `Application.persistentDataPath`에 CSV를 기록한다.
- **Windows Editor 경로**: `C:\Users\<user>\AppData\LocalLow\DefaultCompany\My project (1)\`
- **파일명**: `VR_Experiment_Log_{Control|PostAdaptation}_{timestamp}.csv`
- **컬럼**: `Timestamp, Condition, Event, ReactionTime_ms, TargetPosX/Y/Z, HeadRotX/Y/Z`
- **이벤트**: `Spawn`, `Hit`, `Miss`, `FriendlyFire`

### Phase별 안내 UI

`ExperimentInstructionUI`가 `VRAdaptationManager.OnPhaseChanged`를 구독하여 Phase에 맞는 한국어 안내 텍스트를 표시한다. UI 패널은 HMD 0.25m 앞에 고정 부착 (건물에 가려지지 않음), sortingOrder=10으로 BlackoutCanvas 위에 렌더.

## 알려진 미구현 항목

- `VRAdaptationManager.TriggerHapticPulse()` — 빈 메서드, Phase 1/2 심박 햅틱 미구현
- `m_PlayerSpawnPoint` 미연결 시 Phase 전환 시 위치 리셋 없이 그냥 진행 (경고 없음)
- `m_Phase1NavMission` / `m_Phase2NavMission` 미연결 시 즉시 다음 Phase로 넘어감 (LogWarning 출력)
