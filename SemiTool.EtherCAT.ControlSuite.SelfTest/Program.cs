using SemiTool.EtherCAT.ControlSuite.Models;
using SemiTool.EtherCAT.ControlSuite.Services;

var simulator = new OfflineEquipmentSimulator();
var evaluator = new SafetyInterlockEvaluator();
var teachingProvider = new ReadOnlyTeachingValueProvider();

var teachingPoints = teachingProvider.LoadApprovedTeachingPoints();
Assert(teachingPoints.Count == 0, "티칭값 공급자는 승인 소스 연결 전까지 빈 목록이어야 합니다.");

var powerOn = simulator.CreatePowerOnSnapshot();
var powerOnPermission = evaluator.GetMotionPermission(powerOn, approvedTeachingLoaded: false);
Assert(!powerOnPermission.CanRunOfflineSimulation, "시뮬레이터 연결 전에는 오프라인 사이클도 시작하지 않습니다.");
Assert(!powerOnPermission.CanIssueRealMotion, "승인 티칭값 없이 실제 이동 명령은 금지되어야 합니다.");

var connected = simulator.ConnectOfflineRig();
var connectedPermission = evaluator.GetMotionPermission(connected, approvedTeachingLoaded: false);
Assert(connectedPermission.CanRunOfflineSimulation, "오프라인 시뮬레이터 연결 후에는 조건 검증 사이클을 실행할 수 있어야 합니다.");
Assert(!connectedPermission.CanIssueRealMotion, "오프라인 시뮬레이터 연결만으로 실제 이동 명령을 허용하면 안 됩니다.");

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

Console.WriteLine("SelfTest OK: simulator, interlocks, teaching guard");

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
