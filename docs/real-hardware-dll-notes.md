# Real Hardware DLL Notes

## Public Repository Policy

The public GitHub repository does not contain `IEG3268_Dll.dll`.

This is intentional. The vendor DLL is a machine-local dependency and must not be committed.

## Local Visual Studio / Equipment PC Setup

For actual equipment PC testing, place the local vendor DLL at:

```text
libs/IEG3268_Dll.dll
```

You can also configure an absolute DLL path in the WPF Settings screen.

## Resolution Order

Real Hardware mode resolves the DLL in this order:

1. Absolute path configured in Settings.
2. Relative path from the current working directory.
3. Relative path from the application output directory.
4. Repository-root `libs/IEG3268_Dll.dll`.
5. Output-local `libs/IEG3268_Dll.dll`.

## Load Timing

The DLL is loaded only when Real Hardware mode is selected, hardware control is unlocked, and Connect is clicked.

Simulator mode does not require the DLL.

The app must continue to build, test, and run simulator mode even when the DLL is absent.

## Copy To Output

If `libs/IEG3268_Dll.dll` exists locally, the WPF project copies it to the build output under:

```text
libs/IEG3268_Dll.dll
```

The copy is conditional. Build succeeds when the DLL is missing.

## Architecture Warning

If `BadImageFormatException` occurs, the DLL may be 32-bit/i386 while the app is running as x64.

In that case, run Real Hardware mode using x86 or provide a matching x64 vendor DLL.

The exact required architecture must be confirmed on the school equipment PC.

## Safety Boundary

This repository prepares the WPF app for supervised real-hardware verification, but the new WPF implementation has not yet been verified on real hardware.

Do not add vendor DLLs, real equipment videos, or private machine/customer details to the public repository unless explicitly approved.
