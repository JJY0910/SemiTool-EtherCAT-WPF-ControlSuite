using SemiTool.EtherCAT.ControlSuite.Models;

namespace SemiTool.EtherCAT.ControlSuite.Services;

public sealed class ReadOnlyTeachingValueProvider : ITeachingValueProvider
{
    public IReadOnlyList<TeachingPoint> LoadApprovedTeachingPoints()
    {
        // 실제 티칭값은 PLC 또는 승인된 설정 파일에서 읽어야 합니다.
        // 이 구현은 UI 초기 개발용 경계이며, 임의 좌표/보정값을 절대 생성하지 않습니다.
        return Array.Empty<TeachingPoint>();
    }
}
