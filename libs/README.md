# Local Vendor DLL Folder

Do not commit vendor DLLs.

For real hardware mode on the actual equipment PC or local Visual Studio environment, place the local IEG3268 vendor DLL here:

```text
libs/IEG3268_Dll.dll
```

The public build compiles and runs Simulator mode without this DLL.

If the DLL exists here, the WPF app project conditionally copies it to the output folder under `libs/IEG3268_Dll.dll`.

The DLL remains ignored by git and must not be committed.

You can also set an absolute DLL path in the Settings screen before selecting Real Hardware mode.

If `BadImageFormatException` occurs, the vendor DLL may be 32-bit while the app is running x64. Use an x86 Real Hardware run configuration or provide a matching x64 DLL after confirming the school equipment PC requirements.
