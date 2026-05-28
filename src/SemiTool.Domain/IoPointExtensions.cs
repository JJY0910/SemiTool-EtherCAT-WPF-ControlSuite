namespace SemiTool.Domain;

public static class IoPointExtensions
{
    public static string GetDisplayName(this IoPoint point) => point switch
    {
        IoPoint.TowerRed => "Tower Red",
        IoPoint.TowerYellow => "Tower Yellow",
        IoPoint.TowerGreen => "Tower Green",
        IoPoint.ChamberALamp => "Chamber A Lamp",
        IoPoint.ChamberADoorClose => "Chamber A Door Close",
        IoPoint.ChamberADoorOpen => "Chamber A Door Open",
        IoPoint.ChamberBLamp => "Chamber B Lamp",
        IoPoint.ChamberBDoorClose => "Chamber B Door Close",
        IoPoint.ChamberBDoorOpen => "Chamber B Door Open",
        IoPoint.ChamberCLamp => "Chamber C Lamp",
        IoPoint.ChamberCDoorClose => "Chamber C Door Close",
        IoPoint.ChamberCDoorOpen => "Chamber C Door Open",
        IoPoint.CylinderForward => "Cylinder Forward",
        IoPoint.CylinderBackward => "Cylinder Backward",
        IoPoint.VacuumSuction => "Vacuum Suction",
        IoPoint.VacuumExhaust => "Vacuum Exhaust",
        IoPoint.ChamberADoorOpenSensor => "Chamber A Door Open Sensor",
        IoPoint.ChamberADoorCloseSensor => "Chamber A Door Close Sensor",
        IoPoint.ChamberBDoorOpenSensor => "Chamber B Door Open Sensor",
        IoPoint.ChamberBDoorCloseSensor => "Chamber B Door Close Sensor",
        IoPoint.ChamberCDoorOpenSensor => "Chamber C Door Open Sensor",
        IoPoint.ChamberCDoorCloseSensor => "Chamber C Door Close Sensor",
        IoPoint.CylinderRearSensor => "Cylinder Rear Sensor",
        IoPoint.CylinderFrontSensor => "Cylinder Front Sensor",
        _ => point.ToString()
    };

    public static bool IsInput(this IoPoint point) => point is
        IoPoint.ChamberADoorOpenSensor or
        IoPoint.ChamberADoorCloseSensor or
        IoPoint.ChamberBDoorOpenSensor or
        IoPoint.ChamberBDoorCloseSensor or
        IoPoint.ChamberCDoorOpenSensor or
        IoPoint.ChamberCDoorCloseSensor or
        IoPoint.CylinderRearSensor or
        IoPoint.CylinderFrontSensor;

    public static bool IsOutput(this IoPoint point) => !point.IsInput();
}
