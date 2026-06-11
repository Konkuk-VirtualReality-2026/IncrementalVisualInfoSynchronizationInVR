# VR 멀미 저감을 위한 교차 감각 기반 점진적 시각 동기화 기법

> **건국대학교 가상현실[3221]** | 정근녕, 이수민, 곽경민

---

## 연구 개요

| 항목 | 내용 |
|------|------|
| 연구 명칭 | 교차 감각(Cross-modal) 기반 점진적 시각 동기화를 통한 VR 사이버 멀미 저감 |
| 핵심 가설 | 비시각 감각 선행 → 저정보 시각 → 고정보 시각의 3단계 적응 과정이 멀미를 유의미하게 저감한다 |
| 콘텐츠 유형 | 1인칭 FPS Aim Trainer (Unreal Engine) |
| 측정 도구 | VRSQ, SSQ, SUS(Presence), 행동 데이터 |
| 이론 기반 | 감각 충돌 이론, Rest-frame 가설, 자세 불안정 이론, 통합 모델(Unified Model) |

---

## 강의 핵심 내용 정리 (W13 — Health Effects: Motion Sickness)

> **김형석 교수, 건국대학교 VR LAB** 강의 내용 요약 및 연구 적용 정리

### 1. VR 멀미의 유형 분류

| 유형 | 정의 |
|------|------|
| **Motion Sickness** (광의) | 실제 또는 가상의 움직임 노출 시 발생하는 부정적 증상 |
| **VIMS** (Visually Induced MS) | 물리적 움직임 없이 시각 자극만으로 발생 |
| **Simulator Sickness** | 하드웨어 결함·지연·왜곡 등 시뮬레이션의 기술적 결함으로 발생 |
| **Cybersickness** | VR 몰입 환경에 특화된 멀미 (본 연구의 주 대상) |

### 2. 멀미의 주요 이론

#### 2-1. 감각 충돌 이론 (Sensory Conflict Theory)
- 시각·청각 정보(시뮬레이션)와 전정기관·고유감각 정보(실제 신체) 간 불일치가 멀미 유발
- VR의 근본 과제: 시각·청각 큐를 전정·고유감각 큐와 일치시키는 것
- **본 연구 적용**: 3단계 감각 동기화로 충돌을 진입 시점부터 최소화

#### 2-2. 독소/진화 이론 (Toxin Theory, Treisman 1977)
- 뇌가 감각 불일치를 '신경독소 섭취'로 오인 → 구토·발한의 방어 기제 발동
- 멀미는 진화적 생존 본능이므로 단순 의지로 억제 불가
- **본 연구 적용**: 감각 충돌 자체를 사전에 차단하는 것이 유일한 근본 해결책

#### 2-3. 자세 불안정 이론 (Postural Instability Theory, Riccio & Stoffregen 1991)
- 자세 불안정이 멀미의 원인(CAUSE), 결과가 아님
- 자세 안정 전략을 학습하면 멀미가 감소
- **본 연구 적용**: Phase 1(비시각 단계)에서 촉각·청각으로 공간 기반을 형성해 자세 안정 유도

#### 2-4. Rest-frame 가설 (Prothero & Parker 2003)
- 멀미는 충돌하는 감각 큐 자체가 아니라, 그 큐들이 암시하는 **정지 기준 프레임(Rest Frame)의 충돌**에서 발생
- Rest Frame 제공 시 멀미 감소 가능 (예: 조종석 내부 프레임)
- **본 연구 적용 및 주의사항**: Phase 1(완전 암흑)은 Rest Frame이 없어 방향 상실·불안 유발 가능. 따라서 Phase 1에서도 **최소한의 고정 기준점**(예: 바닥 진동 패턴, 공간 기준 음향)을 제공해야 함

#### 2-5. 안구운동 이론 (Eye Movement Theory)
- 망막에 장면 이미지를 안정시키기 위한 비자연적 안구운동이 멀미 유발
- **Fixation point 제공**이 멀미 감소에 도움
- **본 연구 적용**: Phase 2~3에서 중앙 조준점을 고정 기준으로 활용 고려

#### 2-6. 통합 모델 (Unified Model of Motion Sickness)
```
State(자기운동·세계 상태) → [청각/시각/전정/고유감각/촉각] → 중앙 처리
    ↑                              ↓
Re-afference/feedback    ←  Actions(자세 안정, 안구운동, 생리 반응)
                                   ↑
                         Mental Model(기대치, 기억, Rest Frame)
                         + Efference copy/prediction
```
- 새로운 정신 모델이 생성될 때 멀미 발생 (VR 진입 = 새 정신 모델 생성 시점)
- **본 연구 적용**: 3단계 적응으로 새 정신 모델 형성 비용을 분산·완화

### 3. Vection(자기운동 착각)과 설계 함의

- **Vection**: 물리적 이동 없이 시각 자극만으로 자기이동을 지각하는 현상
- Vection이 항상 멀미를 유발하지는 않음 → 예측 가능한 Vection은 허용
- **회전 운동 > 선형 운동**: 반고리관이 속도와 가속도 모두 감지하므로, 등속 각속도도 멀미 유발
- **FPS Aim Trainer 설계 함의**: 제자리 조준 위주로 설계하되, 캐릭터 좌우 회전(Yaw) 조작을 최소화하거나 제한

### 4. Latency와 멀미

- 사용자는 **100ms 이하** 지연을 인지하지 못하지만 멀미는 이미 발생
- **20ms** 수준의 미세 지연도 감각 충돌 유발
- **120ms 이상**: 수행 능력 회복 불가 → 실험 결과 자체를 오염시킴
- 지연 구성: Tracking delay + Application delay + Rendering delay + Display delay
- **본 연구 적용**: Unreal Engine 렌더링 지연을 최소화(목표 <20ms)하고, 이를 통제변인으로 명시. 모든 조건에서 동일한 렌더링 설정 사용

### 5. 멀미 증상의 4가지 측정 차원

| 차원 | 설명 |
|------|------|
| **강도(Intensity)** | 증상이 얼마나 심각한가 |
| **발현 속도(Rate of Onset)** | 자극 제시 후 증상이 얼마나 빠르게 나타나는가 |
| **회복 속도(Rate of Decay)** | 자극 제거 후 증상이 얼마나 빠르게 감소하는가 |
| **사용자 비율(Percentage of Users)** | 특정 수준 이상의 증상을 겪는 피실험자 비율 |

> 본 연구에서는 강도와 사용자 비율을 주 측정값으로 사용하며, 휴식 시간 기록으로 회복 속도를 보조 측정

### 6. 멀미와 Presence의 트레이드오프 — Golden Ratio

```
사용자 경험 점수
↑
│   ●●●● Warm Amber (High Presence, Low Sickness)
│  ●              ●
│ ●                 ●
│●    ★ Golden Ratio    ●●  ← Clinical Blue (Severe Sickness)
└──────────────────────────→ Immersion Intensity
```

- VR 설계 = **Presence 최대화 + Sickness 최소화**의 최적화 문제
- 몰입도를 높일수록 멀미도 증가하는 트레이드오프 존재
- **본 연구 함의**: 적응 단계가 Presence를 손상시키지 않으면서 멀미를 줄이는지 검증 필요 → Presence 측정이 필수

### 7. VR 사용 후 Aftereffect (재적응)

- 약 10%의 사용자가 VR 종료 후 1~2시간 지각 불안정·방향 감각 상실·플래시백 경험
- 자연 소멸(눈 감고 정지), 능동 재적응(손-눈 협응 과제)으로 회복
- **실험 설계 함의**: 실험 종료 후 피실험자에게 Aftereffect 가능성 안내 및 10분 이상 회복 대기 권장

### 8. 주요 측정 도구 요약

#### VRSQ (VR Sickness Questionnaire, Kim et al. 2018)
- SSQ(16문항)를 VR 환경에 맞게 수정한 9문항 VR 전용 표준 설문지
- 두 하위 요인: **Oculomotor**(안구운동: 일반 불편함·피로·눈의 피로·초점 맞추기 어려움) + **Disorientation**(방향 감각 상실: 두통·머리 꽉 찬 느낌·시야 흐림·어지러움(눈 감았을 때)·현기증)
- 점수 = [Oculomotor 합계 × 가중치] + [Disorientation 합계 × 가중치]

#### SSQ (Simulator Sickness Questionnaire, Kennedy et al. 1993)
- 16문항, 4점 척도(None·Slight·Moderate·Severe)
- 하위 요인: Nausea / Oculomotor / Disorientation
- HMD 환경에서는 VRSQ보다 과대추정 경향이 있으나 세부 비교 목적으로 병행 활용

#### SUS (Slater-Usoh-Steed Questionnaire)
- Presence 측정 표준 도구, 6문항, 1~7점 척도
- 문항 구성: 가상 환경 내 실재감, VR이 현실로 느껴진 정도, 장소 방문 vs. 이미지 시청, 가상/현실 실재감 비교, 기억 구조 유사성, VR 내 있다고 생각한 빈도
- **한계(Presence Paradox)**: "거기 있습니까?" 질문 자체가 현실 귀환을 촉진 → 체험 직후 즉시 작성 필수

---

## 이론적 배경 (연구 기반)

### 사이버 멀미의 원인

- **감각 충돌 이론(Sensory Conflict Theory)**: 시각 정보와 전정기관 정보의 불일치가 멀미를 유발 (Reason & Brand, 1975)
- **예측 코딩(Predictive Coding)**: 뇌는 과거 경험으로 형성한 감각 예측 모델로 입력을 처리하는데, VR 진입 시 현실 세계 기반의 예측 모델과 VR 입력이 즉시 충돌
- **자세 불안정 이론**: 자세 안정 전략 미습득 상태가 멀미의 선행 원인
- **Rest-frame 가설**: 정지 기준 프레임의 충돌이 멀미의 핵심 기제

### 본 연구의 핵심 메커니즘

```
비시각 감각으로 공간 예측 모델 형성(Phase 1)
    → 저정보 시각과 비시각 인지 매칭(Phase 2)
    → 고정보 시각으로 점진적 고도화(Phase 3)
    → 풀 렌더링 환경에서 최소 충돌로 콘텐츠 체험
```

- 뇌가 새로운 VR 공간에 대한 **가상 공간 예측 모델을 먼저 형성**하도록 유도
- 비시각 감각(촉각·청각)은 현실과 가상의 차이가 상대적으로 적어 충돌이 낮음 → 안전한 기반 먼저 확보
- 이후 시각 정보를 단계적으로 연결하여 감각 충돌 최소화

> **기존 연구와의 차이**: 기존 연구는 멀미 발생 후 완화에 집중(vignetting, 동적 FOV 축소 등). 본 연구는 **진입 전 예측 모델 형성**으로 원인 자체를 차단하는 적응 유도(Induced Adaptation) 패러다임

---

## 제안서 타당성 검토

### 타당한 부분

| 요소 | 근거 |
|------|------|
| 3단계 점진적 접근 | 최근 연구(ACM VRST 2025)에서 감각 수준(Sensory Level)이 Presence와 멀미에 유의미한 영향 확인 |
| 멀티센서리 동기화 | 햅틱+시각+청각 동기화가 멀미 저감에 효과적임이 다수 연구에서 확인 |
| 점진적 LOD 전환 | 즉시 전환 대비 부드러운 LOD 블렌딩이 몰입감·안락감 향상에 유효 |
| 노출 시간 점진적 증가 | 강의에서 "Increasing expose time"이 공식 멀미 저감 기법으로 제시됨 |
| 적응 세션 효과 | 반복적 감각 자극 노출로 뇌가 새 환경에 적응 가능함이 확인됨 |

### 주의해야 할 부분

| 우려 사항 | 설명 | 대응 방안 |
|-----------|------|-----------|
| "뇌 가소성" 용어 | 진정한 신경 가소성은 수일~수주 소요; 수분 내 변화는 **감각 교정(Sensory Calibration)** 또는 **지각 기반화(Perceptual Priming)**가 더 정확 | 논문에서 용어 수정 |
| Phase 1 암흑의 Rest-frame 부재 | 완전 시각 차단은 Rest Frame이 없어 방향 상실·불안 유발 가능 (Rest-frame 가설) | Phase 1에 최소 고정 기준점(촉각 진동 패턴, 공간 기준음) 제공 + 사전 스크리닝에서 어둠 민감도 확인 |
| 현실 예측 "소거" 불가 | 분 단위에서 현실 예측치를 약화시키는 것은 어려움 | "가상 공간 예측치 형성"에 집중하는 프레임이 더 타당 |
| 회전 이동 시 멀미 심화 | FPS에서 캐릭터 회전(Yaw)이 선형 이동보다 심한 멀미 유발 (반고리관) | Aim Trainer를 제자리 조준 위주로 설계, 좌우 시점 회전 최소화 |
| Latency 영향 | 렌더링 지연 차이가 조건 간 혼재변인이 될 수 있음 | 모든 조건에서 동일 렌더링 설정, 목표 latency < 20ms 명시 |
| 표본 크기 | 소규모 피실험자 시 통계적 유의성 확보 어려움 | 탐색적 연구임을 명시, 조건 수 최소화 |
| Aftereffect | VR 종료 후 1~2시간 지속 증상 가능 | 피실험자 사후 안내 및 회복 대기 의무화 |

---

## 선행 연구 비교 및 차별점

### 유사 연구 정리

| 연구                        | 접근 방식                        | 한계                       |
| ------------------------- | ---------------------------- | ------------------------ |
| Fernandes & Feiner (2016) | 동적 FOV 축소(vignetting)로 멀미 완화 | 몰입감 저하, 사용자 인식 저하        |
| Weech et al. (2019)       | Presence와 멀미의 상관관계 분석        | 실질적 저감 기법 제시 부족          |
| 점진적 LOD 렌더링 연구            | 씬 내 LOD를 부드럽게 전환하여 불편감 감소    | 콘텐츠 진입 이후의 완화만 다룸        |
| 전정 재활 훈련(VRT)             | 반복 자극으로 어지럼증 재적응             | 치료 목적, VR 온보딩 설계 미적용     |
| PhantomLegs (2019)        | 햅틱 장치로 멀미 저감                 | 별도 하드웨어 필요, 비용 증가        |
| Lee et al. (2018)         | Mask 기반 이미지 처리로 회전 멀미 저감     | 콘텐츠 내 완화 기법, 진입 전 적응 미적용 |

### 본 연구의 차별점

| 구분 | 기존 연구 | 본 연구 |
|------|-----------|---------|
| 개입 시점 | 콘텐츠 **진행 중** 완화 | 콘텐츠 **시작 전** 예측 모델 형성 |
| 감각 전략 | 단일 감각(주로 시각) 조작 | **비시각 → 저시각 → 고시각** 3단계 교차 감각 |
| 하드웨어 의존 | 추가 기기 필요 경우 多 | 기존 VR 컨트롤러 햅틱·스피커만 활용 |
| 콘텐츠 제약 | 특정 장르에 한정 | 온보딩 단계로 분리 → **장르 무관 적용 가능** |
| 연구 패러다임 | 완화(Mitigation) | **적응 유도(Induced Adaptation)** |

---

## 콘텐츠 기획 — Aim Trainer

### 왜 Aim Trainer인가

- 이동 최소화(제자리 조준) → 이동 유발 멀미 및 Vection 변수 통제 가능
- 회전(Yaw) 조작을 최소화하여 반고리관 자극 제어 (강의: 회전 운동이 선형 운동보다 심한 멀미 유발)
- 능동적 조작으로 행동 표준화 용이
- 점수화 가능 → 수행 능력(Performance) 측정 병행 가능

### 게임플레이 규칙

| 타겟 | 모양/색 | 사격 시 |
|------|--------|---------|
| 정상 타겟 | 초록 구체 | **+점수** |
| 금지 타겟 | 빨간 X 구체 | **-점수** |
| 중립 타겟 | 회색 구체 | 점수 없음 (방해 요소) |

### 세션 구성

```
[적응 단계 - 조건별 상이]          [본 콘텐츠 - 모든 조건 동일]
  Phase 1 → Phase 2 → Phase 3   →   Aim Trainer (풀 렌더링, 2분)
```

> **핵심 설계 원칙**: 본 콘텐츠(Aim Trainer)는 **모든 조건에서 동일한 풀 렌더링**으로 진행.
> 적응 단계가 독립변인, Aim Trainer는 측정 환경 — 콘텐츠에 점진적 렌더링을 적용하면 두 효과를 분리할 수 없음.

---

## 실험 설계

### 3단계 적응 프로세스 (독립변인의 처치)

```
Phase 1 │ 완전 암흑 + 햅틱 진동(바닥 기준점) + 공간음향(방향 단서)
        │ → 비시각적으로 공간 구조 추론 + 최소 Rest Frame 확보
        │ (약 30~60초)
        ↓
Phase 2 │ 와이어프레임 / 윤곽선 / 모자이크 수준 시각 제공
        │ → 비시각 인지와 저정보 시각 매칭
        │ (약 30~60초)
        ↓
Phase 3 │ 해상도·텍스처·광원·디테일 점진적 증가
        │ → 풀 렌더링 수준까지 도달
        │ (약 30~60초)
        ↓
     Aim Trainer (풀 렌더링, 2분) → 측정
```

### 실험 조건

| 조건 | 설명 | 처치 |
|------|------|------|
| **A (통제)** | 적응 단계 없음 | VR 진입 즉시 풀 렌더링 Aim Trainer |
| **B (시각만)** | 시각 점진화만 적용 | Phase 1~3 (햅틱·청각 없음) → Aim Trainer |
| **C (전체 감각)** | 시각 + 햅틱 + 청각 모두 적용 | Phase 1~3 (모든 감각) → Aim Trainer |
| **D (참고 비교)** | 기존 기법(vignetting) | 동적 FOV 축소 방식으로 진입 → Aim Trainer |

> 조건 순서: 피실험자별 카운터밸런싱 (Latin Square) 적용
> 예: 피실험자 1 = A→C→D→B, 피실험자 2 = B→A→C→D …

### 변인 정리

**독립변인**
- 적응 단계 유형 (A/B/C/D)
- 감각 모달리티 조합 (시각만 / 시각+청각+촉각)

**종속변인**
- VRSQ 점수 (VR 멀미 전반 — Oculomotor + Disorientation)
- SSQ 세부 점수 (Nausea / Oculomotor / Disorientation 구분)
- SUS 점수 (Presence — Slater-Usoh-Steed 6문항)
- Aim Trainer 점수·반응시간 (수행 능력)
- 멀미 발현까지 소요 시간 (Rate of Onset 보조 측정)
- 조건 간 휴식 시간 길이 (Rate of Decay 보조 측정)

**통제변인**
- 동일 VR 기기 (HMD 모델, 컨트롤러 동일)
- 렌더링 지연 통일 (목표 < 20ms, 조건 간 동일 그래픽 설정)
- 체험 시간 고정 (각 조건 Aim Trainer 2분 + 적응 단계 최대 3분)
- 피실험자 행동 표준화: 동일한 동선, 동일한 조작 방법 사전 교육
- 조건 간 충분한 휴식 (피실험자 자가 판단 + VRSQ 점수 기준선 복귀 후 진행)
- 선행 설문에서 폐소공포증·어둠 민감도·멀미 민감도 스크리닝

### 실험 절차

```
사전 스크리닝
  └─ 폐소공포증, 시력, 이전 VR 경험, 멀미 이력, 어둠 민감도
      ↓
선행 설문지
  └─ 현재 컨디션, 이전 멀미 경험, ITQ(몰입 성향) 측정
      ↓
조작 방법 사전 교육 (모든 피실험자 동일, 비VR 환경에서 실시)
      ↓
조건 1 (카운터밸런싱 순서에 따라)
  └─ 적응 단계 → Aim Trainer 2분
      ↓
VRSQ + SSQ + SUS 측정 (즉시 — Presence Paradox 방지)
      ↓
충분한 휴식 (피실험자 자가 판단 + 증상 기준선 복귀 확인)
      ↓
조건 2, 3, 4 반복
      ↓
완료 설문지
  └─ 총체적 경험 비교, 선호 조건, 자유 서술
      ↓
Aftereffect 안내 + 10분 이상 회복 대기 (자연 소멸: 눈 감고 정지)
```

### 측정 도구

| 도구 | 측정 내용 | 적용 시점 | 비고 |
|------|-----------|-----------|------|
| **VRSQ** | VR 멀미 (Oculomotor + Disorientation) | 각 조건 직후 즉시 | Kim et al. 2018, VR 전용 |
| **SSQ** | 메스꺼움 / 방향감각 / 안구피로 세분화 | 각 조건 직후 | Kennedy et al. 1993 |
| **SUS** | Presence (실재감) — 6문항 | 각 조건 직후 즉시 | Slater-Usoh-Steed |
| **ITQ** | 몰입 성향 (개인차 통제) | 사전 1회 | Witmer & Singer |
| **행동 데이터** | Aim Trainer 점수, 반응시간 | 자동 기록 | Blueprint → CSV |
| **휴식 시간** | 회복 속도(Rate of Decay) 보조 | 조건 간 기록 | 타이머 측정 |
| **완료 설문** | 총체적 경험, 조건 선호도 | 전체 종료 후 | |

---

## 구현 계획 (Unreal Engine)

### 개발 우선순위

```
1단계: VR 기본 환경 + Aim Trainer 완성 (정상/금지/중립 타겟 + 점수 시스템)
2단계: Phase 1 — 완전 암흑 + 컨트롤러 햅틱(기준점 진동) + 공간 음향(Rest Frame 단서)
3단계: Phase 2 — 와이어프레임/윤곽선/모자이크 포스트프로세스 구현
4단계: Phase 3 — 점진적 LOD 전환 + 해상도/광원 단계적 증가 시스템
5단계: 조건 전환 시스템 + 설문 UI 통합 + 데이터 자동 수집 (CSV)
6단계: Latency 최적화 및 측정 (목표 < 20ms)
```

### 기술 구현 포인트

| 기능 | Unreal 구현 방식 |
|------|----------------|
| 완전 암흑 | Post Process Volume (Exposure 최소화) |
| 와이어프레임 뷰 | Custom Post Process Material (Edge Detection) |
| 점진적 LOD | LOD Group + Timeline 블렌딩 |
| 공간 음향 | Unreal MetaSounds + Spatialization |
| 햅틱 진동 | SetHapticsByValue (강도·주파수 타임라인 제어) |
| 자동 데이터 수집 | Blueprint → CSV 저장 |
| Latency 측정 | Oculus Performance HUD / 자체 타임스탬프 로그 |
| FOV 축소 (조건 D) | Post Process Vignette + 동적 Blend Radius |

---

## 참고 문헌 (선행 연구)

### 핵심 선행 연구

1. **Reason, J., & Brand, J. (1975)** — Motion Sickness. Academic Press. *(감각 충돌 이론 원전)*
2. **Treisman, M. (1977)** — Motion sickness: An evolutionary hypothesis. Science. *(독소/진화 이론)*
3. **Riccio, G., & Stoffregen, T. (1991)** — An ecological theory of motion sickness and postural instability. *(자세 불안정 이론)*
4. **Prothero, J., & Parker, D. (2003)** — A unified model of motion sickness. *(Rest-frame 가설)*
5. **Kennedy, R. et al. (1993)** — Simulator Sickness Questionnaire (SSQ). *(측정 도구)*
6. **Kim, H. K. et al. (2018)** — Virtual reality sickness questionnaire (VRSQ). *(VR 전용 측정 도구)*
7. **Slater, M., Usoh, M., & Steed, A. (1995)** — Taking steps: The influence of a walking technique on presence. *(SUS Presence 도구)*
8. **Fernandes, A., & Feiner, S. (2016)** — Combating VR sickness through subtle dynamic field-of-view modification. *(vignetting 기법)*
9. **Weech, S., Kenny, S., & Barnett-Cowan, M. (2019)** — Presence and cybersickness in virtual reality. *(Presence-멀미 관계)*
10. **LaViola, J. (2000)** — A discussion of cybersickness in virtual environments.
11. **Lee, J. et al. (2018)** — Analysis of Video Image based Element for Motion Sickness. Electronic Imaging. *(Mask 기반 처리)*

### 탐색 권장 선행 연구

- ACM VRST 2025: *The Impact of Sensory Levels on Presence and Cybersickness*
- PMC 2023: *Multisensory Data Fusion and Cybersickness in Immersive VR*
- ScienceDirect: *Sensory Conflict Theory using Motion-coupled VR*
- DBpia: [가상현실 콘텐츠의 시각 요인과 사이버 멀미에 관한 연구](https://www-dbpia-co-kr-ssl.proxy.konkuk.ac.kr/journal/detail?nodeId=T15549856)
- IEEE: [Visual Complexity in VR: Implications for Cognitive Load](https://ieeexplore.ieee.org/document/10536293)

### 키워드 (추가 탐색용)

```
cybersickness, sensory conflict, predictive coding VR, gradual visual adaptation,
cross-modal VR, haptic vibration motion sickness, progressive LOD VR,
sensory priming VR onboarding, vestibular rehabilitation VR,
postural instability motion sickness, rest frame VR, vection cybersickness,
VRSQ validation, latency motion sickness threshold
```

---

## TODO

### 연구 설계
- [ ] 피실험자 수 확정 (통계적 유의성 확보 위해 최소 12명 이상 권장)
- [ ] 각 Phase 지속 시간 파일럿 테스트로 결정
- [ ] 카운터밸런싱 순서 표 완성
- [ ] 사전 스크리닝 항목 확정 (폐소공포증·어둠 민감도·멀미 민감도·시력)
- [ ] Phase 1 Rest Frame 제공 방식 확정 (진동 패턴 vs. 공간음 vs. 두 가지 병행)
- [ ] Aim Trainer 캐릭터 회전(Yaw) 제한 여부 결정

### 개발
- [ ] Unreal Engine VR 기본 환경 구성
- [ ] Aim Trainer (타겟 생성·점수 시스템) 구현
- [ ] Phase 1~3 포스트프로세스 및 햅틱/오디오 구현
- [ ] 조건 전환 + 데이터 자동 수집 시스템
- [ ] 설문 UI 통합 (VRSQ + SSQ + SUS)
- [ ] Latency 측정 및 20ms 이하 최적화

### 논문
- [ ] 선행 연구 리뷰 정리 (위 키워드 기반)
- [ ] 가설 및 연구 질문 명확화
- [ ] "뇌 가소성" → "감각 교정(Sensory Calibration)" 용어 수정
- [ ] IRB(연구윤리) 승인 절차 확인
- [ ] VRSQ 채점 방식 확인 및 분석 코드 준비
- [ ] Golden Ratio 관점에서 Presence-Sickness 트레이드오프 논의 섹션 작성
