# Runtime Verification Capture

2026-06-03 Debug build에서 `SemiTool.Hmi.Wpf`를 직접 실행한 뒤 `Step Once`를 네 번 눌러 FOUP A Slot A1 블레이드 전진 단계에 멈춘 화면이다.

- 캡처 파일: `actual-debug-step-once-foup-a-blade-extending.png`
- 기대 상태: `Current station: FOUP A`, `Visual angle: -120 deg`, `Robot: Picking`, `Blade: Extending`, `Z Work / FOUP A Slot A1`
- 목적: Home/Start 위치에서 바로 블레이드가 나가는 것처럼 보이던 런타임 표시를 확인하고, FOUP A 각도 정렬 이후 블레이드 전진으로 표시되는지 검증한다.

## C:\dev actual-run evidence

`dev-actual/` 폴더는 사용자가 Visual Studio에서 여는 실제 작업 경로인 `C:\dev\SemiTool-EtherCAT-WPF-ControlSuite`에서 Debug 빌드 후 `Step Once`로 직접 캡처한 화면이다.

- `02-step-move-to-foup-a.png`: FOUP A 회전 정렬, `Visual angle: -120 deg`, blade retracted
- `03-step-z-work-foup-a.png`: FOUP A 슬롯 높이 이동, `Visual angle: -120 deg`, blade retracted
- `04-step-blade-extending-foup-a.png`: FOUP A 블레이드 전진, `Robot: Picking`, `Blade: Extending`
- `05-step-vacuum-suction-foup-a.png`: 사용자 문제 캡처와 같은 흡착 단계, `Visual angle: -120 deg`, `Vacuum: SuctionOn`
