using SemiTool.EtherCAT.ControlSuite.Models;
using SemiTool.EtherCAT.ControlSuite.Services;

var simulator = new OfflineEquipmentSimulator();
var evaluator = new SafetyInterlockEvaluator();
var commandGate = new CommandGate(evaluator);
var auditLog = new CommandAuditLog();
var scenarioRunner = new OfflineScenarioRunner(commandGate);
var teachingProvider = new ReadOnlyTeachingValueProvider();

var teachingPoints = teachingProvider.LoadApprovedTeachingPoints();
Assert(teachingPoints.Count == 0, "승인 소스 연결 전에는 티칭값 목록이 비어 있어야 합니다.");

var powerOn = simulator.CreatePowerOnSnapshot();
var powerOnPermission = evaluator.GetMotionPermission(powerOn, approvedTeachingLoaded: false);
Assert(!powerOnPermission.CanRunOfflineSimulation, "시뮬레이터 연결 전에는 오프라인 사이클도 시작하지 않습니다.");
Assert(!powerOnPermission.CanIssueRealMotion, "승인 티칭값 없이 실제 이동 명령은 금지되어야 합니다.");

var connected = simulator.ConnectOfflineRig();
var connectedPermission = evaluator.GetMotionPermission(connected, approvedTeachingLoaded: false);
Assert(connectedPermission.CanRunOfflineSimulation, "오프라인 시뮬레이터 연결 후에는 조건 검증 사이클을 실행할 수 있어야 합니다.");
Assert(!connectedPermission.CanIssueRealMotion, "오프라인 시뮬레이터 연결만으로 실제 이동 명령을 허용하면 안 됩니다.");

var offlineCommand = EquipmentCommand.Create(EquipmentCommandType.AdvanceOfflineSimulation, "FOUP A -> CHAMBER A", "SelfTest");
var offlineDecision = commandGate.Evaluate(offlineCommand, connected, approvedTeachingLoaded: false);
Assert(offlineDecision.IsAllowed, "오프라인 검증 명령은 실장비 티칭값 없이도 허용되어야 합니다.");

var realMotionCommand = EquipmentCommand.Create(EquipmentCommandType.IssueRealMotion, "FOUP A -> CHAMBER A", "SelfTest");
var realMotionDecision = commandGate.Evaluate(realMotionCommand, connected, approvedTeachingLoaded: false);
Assert(!realMotionDecision.IsAllowed, "실제 이동 명령은 승인 티칭값 없이는 차단되어야 합니다.");

auditLog.Append(offlineDecision);
auditLog.Append(realMotionDecision);
Assert(auditLog.Records.Count == 2, "명령 감사 로그는 허용/차단 이력을 유지해야 합니다.");
Assert(auditLog.Records.Any(record => record.Allowed), "명령 감사 로그에는 허용 이력이 있어야 합니다.");
Assert(auditLog.Records.Any(record => !record.Allowed), "명령 감사 로그에는 차단 이력이 있어야 합니다.");

var doorOpen = simulator.SetChamberDoorOpen(isOpen: true);
var doorDecision = commandGate.Evaluate(offlineCommand, doorOpen, approvedTeachingLoaded: false);
Assert(!doorDecision.IsAllowed, "챔버 도어가 열린 상태에서는 오프라인 이송 단계도 진행하지 않습니다.");

var doorClosed = simulator.SetChamberDoorOpen(isOpen: false);
Assert(doorClosed.ChamberDoorsClosed, "챔버 도어 닫힘 상태가 스냅샷에 반영되어야 합니다.");

var slotVerified = simulator.VerifySlotMap();
Assert(slotVerified.SlotMap.Count == 10, "FOUP A/B 각각 5단 슬롯맵을 유지해야 합니다.");
Assert(slotVerified.SlotMap.All(slot => slot.Verified), "슬롯맵 검증 후 모든 슬롯은 Verified 상태여야 합니다.");
Assert(slotVerified.SlotMap.Any(slot => slot.State == WaferSlotState.Reserved), "선택 이송 슬롯은 Reserved 상태로 표시되어야 합니다.");

var advanced = simulator.AdvanceCycle("FOUP A -> CHAMBER A");
Assert(advanced.SequenceProgress > 0, "시뮬레이션 스텝 후 진행도가 증가해야 합니다.");

var emergency = simulator.SetEmergencyStop(isPressed: true);
var emergencyPermission = evaluator.GetMotionPermission(emergency, approvedTeachingLoaded: false);
Assert(emergency.EmergencyStop, "비상정지 입력은 스냅샷에 반영되어야 합니다.");
Assert(!emergencyPermission.CanRunOfflineSimulation, "비상정지 중에는 오프라인 사이클도 진행하지 않습니다.");

var nominalScenario = scenarioRunner.RunNominalTransfer("FOUP A -> CHAMBER A");
Assert(nominalScenario.Passed, $"정상 오프라인 이송 시나리오 실패: {nominalScenario.Summary}");

var doorScenario = scenarioRunner.RunDoorOpenBlock("FOUP A -> CHAMBER A");
Assert(doorScenario.Passed, $"도어 열림 차단 시나리오 실패: {doorScenario.Summary}");

Console.WriteLine("SelfTest OK: simulator, command gate, audit log, scenarios, teaching guard");

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
