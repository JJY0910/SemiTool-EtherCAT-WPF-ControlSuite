# Runtime Verification Capture

2026-06-03 Debug build에서 `SemiTool.Hmi.Wpf`를 직접 실행한 뒤 `Step Once`를 네 번 눌러 FOUP A Slot A1 블레이드 전진 단계에 멈춘 화면이다.

- 캡처 파일: `actual-debug-step-once-foup-a-blade-extending.png`
- 기대 상태: `Current station: FOUP A`, `Visual angle: -120 deg`, `Robot: Picking`, `Blade: Extending`, `Z Work / FOUP A Slot A1`
- 목적: Home/Start 위치에서 바로 블레이드가 나가는 것처럼 보이던 런타임 표시를 확인하고, FOUP A 각도 정렬 이후 블레이드 전진으로 표시되는지 검증한다.
