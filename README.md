# SemiTool EtherCAT WPF Control Suite

반도체 웨이퍼 이송 제어 트레이너용 WPF 운전 UI 초기 구현입니다.

## 현재 구현 범위

- 장비 구성도 기반 메인 운전 화면
- CHAMBER A/B/C, FOUP A/B, 중앙 이송부, 전면 제어반, 조작 스위치 박스 상태 표시
- 시퀀스 진행도, 축 상태, 최근 알람 이벤트 표시
- 티칭값 읽기 전용 경계 구성

## 티칭값 보호 원칙

실제 장비 티칭 좌표, 오프셋, 보정값은 임의로 생성하거나 수정하지 않습니다.
UI는 승인된 PLC 또는 설정 소스에서 읽은 값만 표시하도록 구성해야 합니다.

## 빌드

```powershell
dotnet build SemiTool.EtherCAT.ControlSuite.slnx -v minimal
```
