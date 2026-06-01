using SemiTool.EtherCAT.ControlSuite.Models;

namespace SemiTool.EtherCAT.ControlSuite.Services;

public sealed class CommandGate
{
    private readonly SafetyInterlockEvaluator _interlockEvaluator;

    public CommandGate(SafetyInterlockEvaluator interlockEvaluator)
    {
        _interlockEvaluator = interlockEvaluator;
    }

    public CommandDecision Evaluate(EquipmentCommand command, EquipmentSnapshot snapshot, bool approvedTeachingLoaded)
    {
        var checks = _interlockEvaluator.Evaluate(snapshot, approvedTeachingLoaded);
        var permission = _interlockEvaluator.GetMotionPermission(snapshot, approvedTeachingLoaded);

        var isAllowed = command.Type switch
        {
            EquipmentCommandType.CheckConnection => true,
            EquipmentCommandType.CheckServoReady => true,
            EquipmentCommandType.CheckHome => true,
            EquipmentCommandType.ReadSlotMap => snapshot.EtherCatLink,
            EquipmentCommandType.AdvanceOfflineSimulation => permission.CanRunOfflineSimulation,
            EquipmentCommandType.IssueRealMotion => permission.CanIssueRealMotion,
            EquipmentCommandType.StopMotion => true,
            _ => false
        };

        return new CommandDecision(command, isAllowed, BuildReason(command, isAllowed, permission), checks);
    }

    private static string BuildReason(EquipmentCommand command, bool isAllowed, MotionPermission permission)
    {
        if (isAllowed)
        {
            return command.Type == EquipmentCommandType.IssueRealMotion
                ? "실제 이동 명령 허용 전 최종 작업자 확인 필요"
                : "명령 허용";
        }

        return command.Type == EquipmentCommandType.IssueRealMotion
            ? $"실제 이동 명령 차단: {permission.Reason}"
            : $"명령 차단: {permission.Reason}";
    }
}
