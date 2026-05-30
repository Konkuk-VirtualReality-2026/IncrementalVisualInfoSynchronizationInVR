# BasicScene 구현 현황

> 구현 진행 상황을 추적하는 파일. 논의 및 작업 완료 시 업데이트.

---

## LobbyScene

### 확정된 설계

| 항목 | 결정 내용 |
| ---- | --------- |
| 참가자 ID 형식 | 숫자 1~2자리 (1~99) |
| ID 입력 방식 | 숫자 패드 버튼 (0~9 + 지우기) |
| 그룹 뒤로가기 | 불필요 (앱 재시작으로 대체) |
| 실험 시작 방식 | 그룹 선택 후 3초 자동 카운트다운 → BasicScene 로드 |

### UI 흐름

```
[ID 숫자 패드] → 숫자 입력 → [1군 / 2군 선택] → ConfirmPanel(3초 카운트다운) → BasicScene
```

### TODO

- [x] LobbyCanvas WorldSpace + XR 카메라 연결
- [x] XR Origin → Starter Assets XR Rig 교체
- [x] 루트 Main Camera 제거
- [x] 빈 Canvas 오브젝트 제거
- [x] InputField → 숫자 패드 UI로 교체 (0~9 + ⌫, NumpadButton 스크립트)
- [x] LobbyManager 코드 수정 (numpad 로직 + 3초 카운트다운)
- [x] Group1/Group2 버튼 → SelectGroup1/SelectGroup2 OnClick 연결
- [x] ConfirmPanel에 CountdownText 추가
- [ ] 컨트롤러 모델 Quest 3 빌드에서 표시 확인 (실기기 테스트 필요)

---

## BasicScene

> 논의 예정

---
