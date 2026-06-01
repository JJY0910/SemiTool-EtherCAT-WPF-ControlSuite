using SemiTool.EtherCAT.ControlSuite.Models;

namespace SemiTool.EtherCAT.ControlSuite.Services;

public interface IEquipmentAdapter
{
    AdapterConnectionState ConnectionState { get; }

    EquipmentSnapshot ReadSnapshot();

    CommandDecision EvaluateCommand(EquipmentCommand command, bool approvedTeachingLoaded);
}
