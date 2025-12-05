echo %1
adb devices | findstr /R /C:"device$" > nul
if %ERRORLEVEL%==0 (
    adb push "%1\NOT_FGTools" /storage/emulated/0/MelonLoader/com.Mediatonic.FallGuys_client/Mods/
    adb push "%1\NOT FGTools.dll" /storage/emulated/0/MelonLoader/com.Mediatonic.FallGuys_client/Mods/
    adb push "%1\NOT FGTools.pdb" /storage/emulated/0/MelonLoader/com.Mediatonic.FallGuys_client/Mods/
    adb shell am start -n com.Mediatonic.FallGuys_client/com.unity3d.player.UnityPlayerActivity
) else (
    echo ADB not connected.
)
exit 0