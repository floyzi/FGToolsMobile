echo %1
set "PACKAGE=com.flz.notfgtools.dev"

adb devices | findstr /R /C:"device$" > nul
if %ERRORLEVEL%==0 (
    adb push "%1\NOT_FGTools" /storage/emulated/0/MelonLoader/%PACKAGE%/Mods/
    adb push "%1\NOT FGTools.dll" /storage/emulated/0/MelonLoader/%PACKAGE%/Mods/
    adb push "%1\NOT FGTools.pdb" /storage/emulated/0/MelonLoader/%PACKAGE%/Mods/
    adb shell am start -n %PACKAGE%/com.unity3d.player.UnityPlayerActivity
) else (
    echo ADB not connected.
)
exit 0