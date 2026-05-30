# Interview Script

## Korean

```text
기존 프로젝트에서는 WinForms 기반으로 실제 EtherCAT 장비를 제어했습니다. 단순 화면 시연이 아니라 IEG3268 DLL을 통해 EtherCAT 연결, 서보 ON/OFF, Z/Theta 축 Homing과 절대 이동, Digital I/O, 실린더, 진공, 챔버 도어, 타워 램프를 제어했던 프로젝트였습니다.

이번 포트폴리오에서는 그 경험을 그대로 WinForms 화면 변환으로 옮기지 않고, WPF/MVVM 기반의 장비 제어 플랫폼으로 다시 설계했습니다. 실제 장비에서 검증했던 DO/DI 채널, 로봇 포즈, FOUP 슬롯 위치, 타이밍 값을 EquipmentProfile JSON으로 분리했고, 단위 테스트로 값이 바뀌지 않도록 보호했습니다.

UI는 ViewModel과 Command만 담당하고, 실제 시퀀스는 Application Service에서 async/await와 CancellationToken 기반으로 동작합니다. 하드웨어 접근은 IEthercatController 인터페이스 뒤로 숨겼고, 개발 PC에서는 SimulatedEthercatController로 실행할 수 있습니다. 실제 장비 연결은 Real Hardware 모드를 사용자가 명시적으로 선택하고 unlock한 뒤 Connect를 눌렀을 때만 Ieg3268EthercatController가 vendor DLL을 로드합니다.

또한 legacy 코드에서 DO7/DO8 같은 직접 채널 호출과 주석이 충돌하는 위험이 있었기 때문에, 새 구조에서는 application logic에서 raw DO/DI 번호 사용을 금지하고 IoPoint enum과 profile mapping으로만 접근하게 했습니다. Auto scheduler는 PM C -> FOUP B, PM B -> PM C, PM A -> PM B, FOUP A -> PM A 순서의 우선순위를 유지합니다.
```

## English

```text
The legacy project was a WinForms EtherCAT control program that successfully drove real hardware through an IEG3268 vendor DLL. It handled connection, servo control, Z/Theta homing and motion, digital I/O, cylinder, vacuum, chamber doors, tower lamps, wafer transfer, and process recipes.

For this project I rebuilt the concept as a clean WPF/MVVM equipment-control suite instead of converting WinForms screens one-to-one. The real equipment values are preserved in EquipmentProfile.finaltest.json and verified by tests. ViewModels expose commands, Application services orchestrate sequences, and all hardware access goes through IEthercatController.

The default mode is Simulator, so the app can run safely on any developer PC. Real hardware mode is explicit: the operator selects RealHardware, unlocks control, and manually connects. The IEG3268 adapter loads the vendor DLL only at runtime, so the public build does not need the DLL.
```
