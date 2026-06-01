# SemiTool EtherCAT WPF Control Suite

반도체 웨이퍼 이송 제어 트레이너용 WPF 운전 UI 초기 구현입니다.

## 현재 구현 범위

- 장비 구성도 기반 메인 운전 화면
- CHAMBER A/B/C, FOUP A/B, 중앙 이송부, 전면 제어반, 조작 스위치 박스 상태 표시
- 시퀀스 진행도, 축 상태, 최근 알람 이벤트 표시
- 티칭값 읽기 전용 경계 구성
- 집/개발 환경에서 실행 가능한 오프라인 장비 시뮬레이터
- 실제 이동 전 조건을 분리해서 보는 안전 인터록 판정
- FOUP A/B 5단 슬롯맵 상태 표시
- 블레이드 방향, 원점 기준, 챔버 도어 개폐 상태 표시
- 실제 이동 명령 차단을 위한 Command Gate/Audit 구조
- 명령 허용/차단 이력을 확인하는 감사 로그 패널
- 정상 이송/도어 열림 차단 오프라인 시나리오 검증
- 패키지 의존성 없는 자체 검증 실행기

## 티칭값 보호 원칙

실제 장비 티칭 좌표, 오프셋, 보정값은 임의로 생성하거나 수정하지 않습니다.
UI는 승인된 PLC 또는 설정 소스에서 읽은 값만 표시하도록 구성해야 합니다.
오프라인 시뮬레이터의 `SIM-*` 슬롯 식별자는 실제 웨이퍼 ID나 티칭값이 아닙니다.
`config/equipment-map.template.json`은 학교 장비 연결 시 채울 항목의 템플릿이며 실제 IP, 태그, 티칭값을 커밋하면 안 됩니다.

## 빌드

```powershell
dotnet build SemiTool.EtherCAT.ControlSuite.slnx -v minimal
```

## 자체 검증

```powershell
dotnet run --project SemiTool.EtherCAT.ControlSuite.SelfTest\SemiTool.EtherCAT.ControlSuite.SelfTest.csproj
```
