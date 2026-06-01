using SemiTool.EtherCAT.ControlSuite.Models;

namespace SemiTool.EtherCAT.ControlSuite.Services;

public interface ITeachingValueProvider
{
    IReadOnlyList<TeachingPoint> LoadApprovedTeachingPoints();
}
