# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 프로젝트 개요

Meta Quest 3 대상 피험자 간 설계(between-subjects) VR 실험 프로젝트. Unity 6 + URP 기반.
단계적 시각 정보 노출이 VR 멀미 감소 및 조준 수행 능력에 미치는 영향을 측정한다.

- **1군(대조군)**: 처음부터 완전한 시각 → 즉시 AimTrainer
- **2군(실험군)**: 3단계 적응(완전 암흑 → 실루엣 → 완전 렌더) → AimTrainer

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

- `m_DebugFastMode = true` — 모든 Phase 시간을 1/10으로 단축 (빠른 테스트용)
- `m_ForceControlGroup = true` — 적응 Phase를 건너뛰고 AimTrainer로 직행

## 아키텍처

### 실험 흐름

```
LobbyScene  →  ExperimentCondition (static, 씬 간 유지)
                ├─ SelectedGroup: Control | Adaptation
                └─ ParticipantID

BasicScene  →  VRAdaptationManager.Start()
                ├─ 대조군: ControlSequence()     → AimTrainer (120초)
                └─ 실험군: AdaptationSequence()  → Phase1(30s) → Phase2(30s) → Phase3(30s) → AimTrainer (120초)
```

### 시각 피델리티 파이프라인

`VRAdaptationManager`가 글로벌 셰이더 float `_GlobalVisualFidelity` (0→1)를 구동한다.

1. **`GlobalAdaptationEffect`** — Awake 시 씬의 모든 `Renderer` 머티리얼을 `VR/AdaptationProgressive` (셰이더: `Assets/Shaders/ProgressiveLOD.shader`)로 교체하고 원본을 저장. 오브젝트명에 `vignette`, `sky`, `ui` 등이 포함되거나 Layer 5(UI)인 경우 제외.
2. **`PhaseOutlineFeature`** (URP Renderer Feature, 셰이더: `Hidden/VRAdaptation/PhaseOutline` = `Assets/Shaders/PhaseOutline.shader`) — `_GlobalVisualFidelity`가 [0.28, 0.72) 구간(Phase 2)일 때만 활성화. RenderGraph API + Compatibility Mode 양쪽 구현.
3. **Phase 1 완전 암흑** — XR 카메라 자식 `BlackoutCanvas` (z=0.31)로 전체 화면을 덮고, 카메라 배경을 `SolidColor/Black`으로 전환해 스카이박스 투과를 차단.

`VRAdaptationManager`는 반드시 `GlobalAdaptationEffect`보다 먼저 실행되어야 하며, `[DefaultExecutionOrder(-100)]`으로 보장한다.

### 주요 싱글톤

| 클래스 | 게임오브젝트 | 역할 |
|--------|------------|------|
| `VRAdaptationManager` | `VR_Adaptation_Manager` | Phase 시퀀스 제어, 피델리티 갱신, 이벤트 버스(`OnPhaseChanged`) |
| `GlobalAdaptationEffect` | `VR_Adaptation_Manager` | 셰이더 교체 / 원본 복원 |
| `AimTargetManager` | `Aim_Trainer_Manager` | 타겟 스폰, 히트/미스 콜백 |
| `ExperimentDataLogger` | `Aim_Trainer_Manager` | CSV 로깅 |
| `AimTrainerHUD` | `AimTrainerHUD` | World Space 점수/정확도/타이머 UI |

### 발사 시스템

`RaycastWeapon` + `VRGunController`가 Right Hand Controller에 부착된다. 발사 액션: `XRI Right Hand Interaction/Activate`. 레이캐스트는 `m_TargetLayer`를 사용하며 AimTarget 프리팹의 레이어를 포함해야 한다. `m_FireClip`, `m_HitClip`은 Inspector에서 수동 할당 필요.

### 실험 데이터 저장 위치

`ExperimentDataLogger`가 `Application.persistentDataPath`에 CSV를 기록한다.
- **Windows Editor 경로**: `C:\Users\<user>\AppData\LocalLow\DefaultCompany\My project (1)\`
- **파일명**: `VR_Experiment_Log_{Control|PostAdaptation}_{timestamp}.csv`
- **컬럼**: `Timestamp, Condition, Event, ReactionTime_ms, TargetPosX/Y/Z, HeadRotX/Y/Z`
- **이벤트**: `Spawn`, `Hit`, `Miss`

### Phase별 안내 UI

`ExperimentInstructionUI`가 `VRAdaptationManager.OnPhaseChanged`를 구독하여 Phase에 맞는 한국어 안내 텍스트를 표시한다. UI 패널은 HMD를 부드럽게 추적한다.

## 알려진 미구현 항목

- `VRAdaptationManager.TriggerHapticPulse()` 가 빈 메서드 — Phase 1/2 심박 햅틱 미구현.
- `RaycastWeapon`의 `m_FireClip`, `m_HitClip`, `m_TargetLayer`는 Inspector에서 수동 설정 필요.
