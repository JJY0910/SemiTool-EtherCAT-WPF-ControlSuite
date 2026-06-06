## Summary

-

## Related Requirements

-

## Changed Files

-

## Scope / Safety Boundaries

- [ ] Preserved equipment values were not changed.
- [ ] No startup auto-connect, auto-run, auto-home, auto-motion, or output activation was added.
- [ ] Real hardware behavior was not claimed unless supervised equipment validation is documented.
- [ ] Vendor DLL usage remains inside `Ieg3268EthercatController`.

## Validation Results

```powershell
dotnet restore SemiTool.EtherCAT.WPF.ControlSuite.sln
dotnet build SemiTool.EtherCAT.WPF.ControlSuite.sln --configuration Release --no-restore
dotnet test SemiTool.EtherCAT.WPF.ControlSuite.sln --configuration Release --no-build --no-restore
```

## GitHub Checks Result

-

## Known Limitations

-

## Next Recommended Step

-
