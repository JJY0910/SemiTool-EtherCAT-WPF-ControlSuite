using SemiTool.EtherCAT.ControlSuite.Models;

namespace SemiTool.EtherCAT.ControlSuite.Services;

public sealed class SafetyInterlockEvaluator
{
    public IReadOnlyList<InterlockCheck> Evaluate(EquipmentSnapshot snapshot, bool approvedTeachingLoaded)
    {
        return new[]
        {
            new InterlockCheck(
                "EtherCAT 링크",
                "I/O 커플러",
                snapshot.EtherCatLink ? "오프라인 시뮬레이터 링크 정상" : "실장비 또는 시뮬레이터 링크가 없습니다.",
                snapshot.EtherCatLink,
                InterlockSeverity.Blocker),
            new InterlockCheck(
                "비상정지 해제",
                "조작 스위치 박스",
                snapshot.EmergencyStop ? "EMG SW 입력 활성" : "EMG SW 해제 상태",
                !snapshot.EmergencyStop,
                InterlockSeverity.Blocker),
            new InterlockCheck(
                "서보 준비",
                "서보 드라이브",
                snapshot.ServoReady ? "서보 준비 신호 확인" : "서보 준비 신호 대기",
                snapshot.ServoReady,
                InterlockSeverity.Blocker),
            new InterlockCheck(
                "원점 확인",
                "모션 축",
                snapshot.AxisHomed ? "홈 완료 조건 확인" : "홈 완료 전 티칭 위치 이동 금지",
                snapshot.AxisHomed,
                InterlockSeverity.Blocker),
            new InterlockCheck(
                "챔버 도어",
                "CHAMBER A/B/C",
                snapshot.ChamberDoorsClosed ? "챔버 도어 닫힘" : "챔버 도어 상태 확인 필요",
                snapshot.ChamberDoorsClosed,
                InterlockSeverity.Blocker),
            new InterlockCheck(
                "FOUP 감지",
                "FOUP A/B",
                snapshot.FoupCassettePresent ? "카세트 감지됨" : "FOUP 카세트 감지 대기",
                snapshot.FoupCassettePresent,
                InterlockSeverity.Blocker),
            new InterlockCheck(
                "슬롯맵 검증",
                "FOUP 슬롯 센서",
                snapshot.SlotMapVerified ? "슬롯맵 확인 완료" : "슬롯별 웨이퍼 유무 확인 필요",
                snapshot.SlotMapVerified,
                InterlockSeverity.Warning),
            new InterlockCheck(
                "진공 픽업",
                "Vacuum Pickup Head",
                snapshot.VacuumReady ? "진공 회로 준비" : "진공 패드/센서 확인 필요",
                snapshot.VacuumReady,
                InterlockSeverity.Blocker),
            new InterlockCheck(
                "이송 경로",
                "중앙 이송부",
                snapshot.RouteClear ? "회전/선형 이송 경로 클리어" : "경로 간섭 또는 홀드 상태",
                snapshot.RouteClear,
                InterlockSeverity.Blocker),
            new InterlockCheck(
                "티칭값 승인 소스",
                "PLC/승인 설정",
                approvedTeachingLoaded ? "승인된 티칭 데이터 읽기 가능" : "승인된 티칭 데이터 미연결 - 실제 이동 명령 금지",
                approvedTeachingLoaded,
                InterlockSeverity.Blocker)
        };
    }

    public MotionPermission GetMotionPermission(EquipmentSnapshot snapshot, bool approvedTeachingLoaded)
    {
        var checks = Evaluate(snapshot, approvedTeachingLoaded);
        var hardwareBlockers = checks
            .Where(check => check.Severity == InterlockSeverity.Blocker && !check.IsSatisfied && check.Name != "티칭값 승인 소스")
            .ToList();

        if (hardwareBlockers.Count > 0)
        {
            return new MotionPermission(false, false, $"{hardwareBlockers[0].Name} 조건 미충족");
        }

        if (!approvedTeachingLoaded)
        {
            return new MotionPermission(true, false, "오프라인 시뮬레이션 가능, 실제 이동 명령은 티칭값 승인 소스 연결 후 가능");
        }

        return new MotionPermission(true, true, "실제 이동 전 최종 작업자 확인 필요");
    }
}
