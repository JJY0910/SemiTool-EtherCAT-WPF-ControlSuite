# Full Pipeline Operator Review

이 문서는 `docs/debug/latest/full-pipeline` 캡처 기준으로 3D Machine Twin의 웨이퍼 이송 파이프라인을 작업자 관점에서 재검증한 기록이다.
실제 EtherCAT 장비는 학교 현장에 있어 연결 검증하지 않았고, 아래 판정은 WPF 3D 렌더 캡처 하네스와 시뮬레이터 상태 추적 기준이다.

## 판정 요약

- PASS: 시작 상태는 FOUP A 5/5, FOUP B 0/5이고 완료 상태는 FOUP A 0/5, FOUP B 5/5이다.
- PASS: W01-W05 5장 모두 `FOUP A -> Chamber A -> Chamber B -> Chamber C -> FOUP B` 순서로 이동한다.
- PASS: Home / Start 상태에서 블레이드는 Retracted 상태이며, FOUP/Chamber 스테이션 방향으로 회전한 뒤 슬롯/공정 높이에서 전진한다.
- PASS: FOUP A에서 웨이퍼를 픽업하면 해당 슬롯이 비고, 블레이드 위 웨이퍼가 표시된 뒤 다음 장비 위치로 이동한다.
- PASS: Chamber A/B/C에는 문 개방 상태, 웨이퍼 점유 디스크, 공정 진행 인디케이터가 캡처된다.
- PASS: 최종 이송 완료 후 W01-W05는 FOUP B B1-B5 슬롯에 순서대로 적재된다.
- LIMIT: 실제 장비 티칭값과 EtherCAT 실기 연결은 변경하거나 검증하지 않았다.

## 핵심 캡처 확인점

- `full-pipeline-contact-sheet.png`: 대표 14장 캡처를 한 장으로 묶은 전체 파이프라인 검증 이미지.
- `screenshots/000-startup-simulator.png`: 초기 상태. FOUP A 5장 적재, FOUP B 비어 있음, 블레이드 Retracted.
- `screenshots/001-move-to-foup-a-slot-a1.png`: Home에서 바로 전진하지 않고 FOUP A A1 방향으로 먼저 회전한 상태.
- `screenshots/005-w01-on-blade-from-foup-a-slot-a1.png`: A1 웨이퍼가 FOUP A에서 빠지고 블레이드 위에 안착한 상태.
- `screenshots/014-w01-placed-at-chamber-a.png`: W01이 Chamber A에 배치되고 챔버 점유 표시가 켜진 상태.
- `screenshots/019-chamber-a-processing-w01.png`: Chamber A 공정 진행 인디케이터 표시.
- `screenshots/038-w01-placed-at-chamber-b.png`: W01이 Chamber B로 이동해 배치된 상태.
- `screenshots/043-chamber-b-processing-w01.png`: Chamber B 공정 진행 인디케이터 표시.
- `screenshots/081-w01-placed-at-chamber-c.png`: W01이 Chamber C로 이동해 배치된 상태.
- `screenshots/086-chamber-c-processing-w01.png`: Chamber C 공정 진행 인디케이터 표시.
- `screenshots/103-w01-placed-at-foup-b-slot-b1.png`: W01이 FOUP B B1 슬롯에 적재된 상태.
- `screenshots/306-w05-on-blade-from-foup-a-slot-a5.png`: 마지막 W05가 FOUP A A5에서 픽업된 상태.
- `screenshots/428-w05-placed-at-foup-b-slot-b5.png`: W05가 FOUP B B5에 적재된 상태.
- `screenshots/431-sequence-complete-foup-b-5-5.png`: 전체 완료. FOUP A 0/5, FOUP B 5/5.
- `screenshots/432-reset-safe-state.png`: Reset 후 안전 상태.

## 웨이퍼별 이동 증거

| Wafer | FOUP A Pick | Chamber A | Chamber B | Chamber C | FOUP B Place |
| --- | --- | --- | --- | --- | --- |
| W01 | `005-w01-on-blade-from-foup-a-slot-a1.png` | `014-w01-placed-at-chamber-a.png` | `038-w01-placed-at-chamber-b.png` | `081-w01-placed-at-chamber-c.png` | `103-w01-placed-at-foup-b-slot-b1.png` |
| W02 | `048-w02-on-blade-from-foup-a-slot-a2.png` | `057-w02-placed-at-chamber-a.png` | `124-w02-placed-at-chamber-b.png` | `167-w02-placed-at-chamber-c.png` | `189-w02-placed-at-foup-b-slot-b2.png` |
| W03 | `134-w03-on-blade-from-foup-a-slot-a3.png` | `143-w03-placed-at-chamber-a.png` | `210-w03-placed-at-chamber-b.png` | `253-w03-placed-at-chamber-c.png` | `275-w03-placed-at-foup-b-slot-b3.png` |
| W04 | `220-w04-on-blade-from-foup-a-slot-a4.png` | `229-w04-placed-at-chamber-a.png` | `296-w04-placed-at-chamber-b.png` | `339-w04-placed-at-chamber-c.png` | `361-w04-placed-at-foup-b-slot-b4.png` |
| W05 | `306-w05-on-blade-from-foup-a-slot-a5.png` | `315-w05-placed-at-chamber-a.png` | `382-w05-placed-at-chamber-b.png` | `406-w05-placed-at-chamber-c.png` | `428-w05-placed-at-foup-b-slot-b5.png` |

## 검증 기준

- FOUP 슬롯은 5층으로 유지되어야 하며, 픽업된 슬롯은 UI에서 Empty로 바뀌어야 한다.
- 챔버 문은 블레이드 진입 방향을 향해야 하며, 진입 중에는 열린 상태가 UI에 드러나야 한다.
- 챔버 내부 웨이퍼는 문 뒤로 뚫고 나가는 형태가 아니라 챔버 공정 공간 안에 있어야 한다.
- 블레이드는 Home에서 전진하지 않고, 목표 스테이션 각도와 Z 작업 위치가 잡힌 후 전진해야 한다.
- 실제 티칭값은 사용자가 정한 값으로 유지하며, UI 검증을 위해 임의 수정하지 않는다.
