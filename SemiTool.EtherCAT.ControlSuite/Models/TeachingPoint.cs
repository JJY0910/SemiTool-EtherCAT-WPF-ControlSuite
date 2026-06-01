namespace SemiTool.EtherCAT.ControlSuite.Models;

public sealed record TeachingPoint(
    // 티칭 포인트 이름입니다. 예: 승인된 소스의 CHAMBER_A_LOAD 같은 태그명.
    string Name,

    // 티칭 포인트가 속한 장비 위치입니다. 예: CHAMBER A, FOUP A, 중앙 이송부.
    string Station,

    // 값을 읽어 온 승인 소스입니다. PLC 태그, 검증된 설정 파일, 장비 백업 데이터만 허용합니다.
    string Source,

    // 읽기/검증 상태입니다. 좌표값 자체를 UI에서 생성하거나 수정하지 않습니다.
    string Status);
