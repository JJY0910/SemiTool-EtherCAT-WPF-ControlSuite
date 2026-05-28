# Local Vendor DLL Folder

Do not commit vendor DLLs.

For real hardware mode, place the local IEG3268 vendor DLL here:

```text
libs/IEG3268_Dll.dll
```

The public build compiles and runs Simulator mode without this DLL. Real Hardware mode reports a clear error if the DLL is missing.
