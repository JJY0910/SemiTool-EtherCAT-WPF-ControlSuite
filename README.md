# SemiTool EtherCAT WPF Control Suite

반도체 웨이퍼 이송 제어 트레이너용 WPF HMI 프로젝트입니다.

## 현재 구현 범위

- 네이티브 WPF `Viewport3D` 기반 머신 트윈 화면
- CHAMBER A/B/C, FOUP A/B, 중앙 회전 이송부, 블레이드, 웨이퍼, 타워 라이트 3D 표시
- HOME 기준 회전, FOUP 정렬, 블레이드 전진, 진공 픽업, 챔버 이송, 배치 순서 표시
- FOUP A/B 5단 슬롯맵과 현재 웨이퍼 상태 표시
- 챔버 도어 열림/닫힘 인터록 상태 표시
- 명령 허용/차단 이력을 확인하는 Command Gate/Audit 로그
- 실제 장비가 없는 개발 환경에서 실행 가능한 오프라인 장비 시뮬레이터
- 정상 이송, 도어 열림 차단, 티칭값 보호 조건을 확인하는 SelfTest 프로젝트

## 티칭값 보호 원칙

실제 장비 티칭 좌표, 오프셋, 보정값은 임의 생성하거나 수정하지 않습니다.

UI의 3D 각도와 블레이드 길이는 화면 표시용 값이며 실제 모터 좌표나 직선축 티칭값이 아닙니다.
실제 EtherCAT 장비 연결 시에는 승인된 설정 소스에서 읽은 값만 표시해야 합니다.
오프라인 시뮬레이터의 `SIM-*` 슬롯 정보는 실제 웨이퍼 ID나 티칭값이 아닙니다.
`config/equipment-map.template.json`은 학교 장비 연결 시 채울 항목의 템플릿이며 실제 IP, 태그, 티칭값은 커밋하지 않습니다.

## 빌드

```powershell
dotnet build SemiTool.EtherCAT.ControlSuite.slnx -v minimal
```

## 자체 검증

```powershell
dotnet run --project SemiTool.EtherCAT.ControlSuite.SelfTest\SemiTool.EtherCAT.ControlSuite.SelfTest.csproj
```
