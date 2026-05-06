

## Para instalar manualmente vía ADB
powershelladb install output\multaapp\com.motordsl.multaapp-Signed.apk

PS E:\repos\tup\aplicada\2025\utn\ia\Ejemplos_Maui_PrintThermal_Driver> $env:PATH += ";C:\Program Files (x86)\Android\android-sdk\platform-tools"   latform-tools"
PS E:\repos\tup\aplicada\2025\utn\ia\Ejemplos_Maui_PrintThermal_Driver> adb logcat -c
- waiting for device -
PS E:\repos\tup\aplicada\2025\utn\ia\Ejemplos_Maui_PrintThermal_Driver> adb logcat -c
- waiting for device -
PS E:\repos\tup\aplicada\2025\utn\ia\Ejemplos_Maui_PrintThermal_Driver>
PS E:\repos\tup\aplicada\2025\utn\ia\Ejemplos_Maui_PrintThermal_Driver> adb logcat -c                                              ee13f6.MainActivi
PS E:\repos\tup\aplicada\2025\utn\ia\Ejemplos_Maui_PrintThermal_Driver> adb shell am start -n com.motordsl.multaapp/crc64b3e95085a2ee13f6.MainActivity
Starting: Intent { cmp=com.motordsl.multaapp/crc64b3e95085a2ee13f6.MainActivity }
Error type 3
Error: Activity class {com.motordsl.multaapp/crc64b3e95085a2ee13f6.MainActivity} does not exist.                                   me|System.err" | 
PS E:\repos\tup\aplicada\2025\utn\ia\Ejemplos_Maui_PrintThermal_Driver> adb logcat *:E | Select-String "motordsl|FATAL|AndroidRuntime|System.err" | Select-Object -First 50
PS E:\repos\tup\aplicada\2025\utn\ia\Ejemplos_Maui_PrintThermal_Driver> adb shell pm list packages | Select-String "motordsl"

package:com.motordsl.multaapp
package:com.motordsl.sampleapp


PS E:\repos\tup\aplicada\2025\utn\ia\Ejemplos_Maui_PrintThermal_Driver> adb shell pm dump com.motordsl.multaapp | Select-String "Activity" | Select-Object -First 10

Activity Resolver Table:
        320a05c com.motordsl.multaapp/crc6483bd6c7d9597e925.MainActivity filter 8e87f65
DUMP OF SERVICE activity:
ACTIVITY MANAGER SETTINGS (dumpsys activity settings) activity_manager_constants:
  service_max_inactivity=1800000
  service_bg_activity_start_timeout=10000
  default_background_activity_starts_enabled=false
ACTIVITY MANAGER ALLOWED ASSOCIATION STATE (dumpsys activity allowed-associations)
ACTIVITY MANAGER PENDING INTENTS (dumpsys activity intents)
ACTIVITY MANAGER BROADCAST STATE (dumpsys activity broadcasts)


objeto

PS E:\repos\tup\aplicada\2025\utn\ia\Ejemplos_Maui_PrintThermal_Driver> adb logcat -c
PS E:\repos\tup\aplicada\2025\utn\ia\Ejemplos_Maui_PrintThermal_Driver> adb shell am start -n com.motordsl.multaapp/crc6483bd6c7d9597e925.MainActivity
Starting: Intent { cmp=com.motordsl.multaapp/crc6483bd6c7d9597e925.MainActivity }
PS E:\repos\tup\aplicada\2025\utn\ia\Ejemplos_Maui_PrintThermal_Driver> adb logcat | Select-String "AndroidRuntime|FATAL|System.err|motordsl|QuestPDF|SkiaSharp" | Select-Object -First 60

03-31 09:13:21.436  1877  6856 I ActivityTaskManager: START u0 {flg=0x10000000 
cmp=com.motordsl.multaapp/crc6483bd6c7d9597e925.MainActivity} from uid 2000
03-31 09:13:21.544  1877  1924 I ActivityManager: Start proc 10994:com.motordsl.multaapp/u0a356 for next-top-activity
{com.motordsl.multaapp/crc6483bd6c7d9597e925.MainActivity}
03-31 09:13:21.550  1877  6856 D CoreBackPreview: Window{a25e5a5 u0 Splash Screen com.motordsl.multaapp}: Setting back callback    
OnBackInvokedCallbackInfo{mCallback=android.window.IOnBackInvokedCallback$Stub$Proxy@a05b188, mPriority=0}
03-31 09:13:21.964 10994 10994 D nativeloader: Configuring clns-4 for other apk 
/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/base.apk. target_sdk_version=36,
uses_libraries=, library_path=/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64:/data/ 
app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/base.apk!/lib/arm64-v8a,
permitted_path=/data:/mnt/expand:/data/user/0/com.motordsl.multaapp
03-31 09:13:21.978 10994 10994 V GraphicsEnvironment: ANGLE Developer option for 'com.motordsl.multaapp' set to: 'default'
03-31 09:13:21.978 10994 10994 V GraphicsEnvironment: ANGLE GameManagerService for com.motordsl.multaapp: false
03-31 09:13:22.003 10994 10994 D nativeloader: Load
/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libmonosgen-2.0.so using class       
loader ns clns-4
(caller=/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/base.apk!classes2.dex): ok
03-31 09:13:22.015 10994 10994 D nativeloader: Load
/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libxamarin-app.so using class        
loader ns clns-4
(caller=/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/base.apk!classes2.dex): ok
03-31 09:13:22.023 10994 10994 D nativeloader: Load
/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libmonodroid.so using class loader   
ns clns-4 (caller=/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/base.apk!classes2.dex): ok   
03-31 09:13:22.033 10994 10994 W monodroid: Failed to create directory 
'/data/user/0/com.motordsl.multaapp/files/.__override__/arm64-v8a'. File exists
03-31 09:13:22.033 10994 10994 W monodroid: Creating public update directory:
`/data/user/0/com.motordsl.multaapp/files/.__override__/arm64-v8a`
03-31 09:13:22.142 10994 10994 D nativeloader: Load /data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2 
JfA==/lib/arm64/libSystem.Security.Cryptography.Native.Android.so using class loader ns clns-4
(caller=/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/base.apk!classes2.dex): ok
03-31 09:13:22.156 10994 10994 D nativeloader: Load
/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libxamarin-debug-app-helper.so       
using class loader ns clns-4
(caller=/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/base.apk!classes2.dex): ok
03-31 09:13:22.157 10994 10994 W monodroid-assembly: open_from_bundles: failed to load bundled assembly MotorDsl.MultaApp.dll
03-31 09:13:22.496 10994 10994 W monodroid-assembly: open_from_bundles: failed to load bundled assembly QuestPDF.dll
03-31 09:13:22.506 10994 10994 W monodroid-assembly: open_from_bundles: failed to load bundled assembly MotorDsl.Core.dll
03-31 09:13:22.510 10994 10994 W monodroid-assembly: open_from_bundles: failed to load bundled assembly MotorDsl.Extensions.dll    
03-31 09:13:22.519 10994 10994 E monodroid-assembly: Could not load library
'/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libQuestPdfSkia.so'. dlopen
failed: library "libstdc++.so.6" not found: needed by
/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libQuestPdfSkia.so in namespace      
clns-4
03-31 09:13:22.519 10994 10994 W monodroid-assembly: Shared library 'QuestPdfSkia' not loaded, p/invoke
'check_compatibility_by_calculating_sum' may fail
03-31 09:13:22.523 10994 10994 E monodroid-assembly: Could not load library
'/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libQuestPdfSkia.so'. dlopen
failed: library "libstdc++.so.6" not found: needed by
/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libQuestPdfSkia.so in namespace      
clns-4
03-31 09:13:22.523 10994 10994 E monodroid-assembly: Could not load library
'/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libQuestPdfSkia.so'. dlopen
failed: library "libstdc++.so.6" not found: needed by
/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libQuestPdfSkia.so in namespace      
clns-4
03-31 09:13:22.525 10994 10994 E monodroid-assembly: Could not load library 
'/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libQuestPdfSkia.so'. dlopen
failed: library "libstdc++.so.6" not found: needed by
/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libQuestPdfSkia.so in namespace      
clns-4
03-31 09:13:22.526 10994 10994 E monodroid-assembly: Could not load library
'/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libQuestPdfSkia.so'. dlopen
failed: library "libstdc++.so.6" not found: needed by
/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libQuestPdfSkia.so in namespace      
clns-4
03-31 09:13:22.530 10994 10994 E monodroid-assembly: Could not load library
'/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libQuestPdfSkia.so'. dlopen
failed: library "libstdc++.so.6" not found: needed by
/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libQuestPdfSkia.so in namespace      
clns-4
03-31 09:13:22.530 10994 10994 W monodroid-assembly: Shared library 'QuestPdfSkia' not loaded, p/invoke
'check_compatibility_by_calculating_sum' may fail
03-31 09:13:22.531 10994 10994 E monodroid-assembly: Could not load library
'/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libQuestPdfSkia.so'. dlopen
failed: library "libstdc++.so.6" not found: needed by
/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libQuestPdfSkia.so in namespace      
clns-4
03-31 09:13:22.532 10994 10994 E monodroid-assembly: Could not load library
'/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libQuestPdfSkia.so'. dlopen
failed: library "libstdc++.so.6" not found: needed by
/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libQuestPdfSkia.so in namespace      
clns-4
03-31 09:13:22.533 10994 10994 E monodroid-assembly: Could not load library
'/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libQuestPdfSkia.so'. dlopen
failed: library "libstdc++.so.6" not found: needed by
/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libQuestPdfSkia.so in namespace      
clns-4
03-31 09:13:22.534 10994 10994 E monodroid-assembly: Could not load library
'/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libQuestPdfSkia.so'. dlopen
failed: library "libstdc++.so.6" not found: needed by
/data/app/~~ogzw9AAd1p6L1rPdj1RPzw==/com.motordsl.multaapp-NxOzQyCnlpr-K3Sv_K2JfA==/lib/arm64/libQuestPdfSkia.so in namespace      
clns-4
03-31 09:13:22.722 10994 10994 D AndroidRuntime: Shutting down VM
03-31 09:13:22.724 10994 10994 E AndroidRuntime: FATAL EXCEPTION: main
03-31 09:13:22.724 10994 10994 E AndroidRuntime: Process: com.motordsl.multaapp, PID: 10994
03-31 09:13:22.724 10994 10994 E AndroidRuntime: android.runtime.JavaProxyThrowable: [System.TypeInitializationException]: The     
type initializer for 'QuestPDF.Settings' threw an exception.
03-31 09:13:22.724 10994 10994 E AndroidRuntime:        at MotorDsl.MultaApp.MauiProgram.CreateMauiApp + 0x1(Unknown Source)       
03-31 09:13:22.724 10994 10994 E AndroidRuntime:        at MotorDsl.MultaApp.MainApplication.CreateMauiApp + 0x0(Unknown Source)   
03-31 09:13:22.724 10994 10994 E AndroidRuntime:        at Microsoft.Maui.MauiApplication.OnCreate + 0xb(Unknown Source)
03-31 09:13:22.724 10994 10994 E AndroidRuntime:        at Android.App.Application.n_OnCreate + 0xe(Unknown Source)
03-31 09:13:22.724 10994 10994 E AndroidRuntime:        at crc6488302ad6e9e4df1a.MauiApplication.n_onCreate(Native Method)
03-31 09:13:22.724 10994 10994 E AndroidRuntime:        at crc6488302ad6e9e4df1a.MauiApplication.onCreate(MauiApplication.java:27) 
03-31 09:13:22.724 10994 10994 E AndroidRuntime:        at
android.app.Instrumentation.callApplicationOnCreate(Instrumentation.java:1279)
03-31 09:13:22.724 10994 10994 E AndroidRuntime:        at android.app.ActivityThread.handleBindApplication(ActivityThread.java:6988)
03-31 09:13:22.724 10994 10994 E AndroidRuntime:        at android.app.ActivityThread.-$$Nest$mhandleBindApplication(Unknown Source:0)
03-31 09:13:22.724 10994 10994 E AndroidRuntime:        at android.app.ActivityThread$H.handleMessage(ActivityThread.java:2238)    
03-31 09:13:22.724 10994 10994 E AndroidRuntime:        at android.os.Handler.dispatchMessage(Handler.java:111)
03-31 09:13:22.724 10994 10994 E AndroidRuntime:        at android.os.Looper.loopOnce(Looper.java:238)
03-31 09:13:22.724 10994 10994 E AndroidRuntime:        at android.os.Looper.loop(Looper.java:357)
03-31 09:13:22.724 10994 10994 E AndroidRuntime:        at android.app.ActivityThread.main(ActivityThread.java:8149)
03-31 09:13:22.724 10994 10994 E AndroidRuntime:        at java.lang.reflect.Method.invoke(Native Method)
03-31 09:13:22.724 10994 10994 E AndroidRuntime:        at
com.android.internal.os.RuntimeInit$MethodAndArgsCaller.run(RuntimeInit.java:548)
03-31 09:13:22.724 10994 10994 E AndroidRuntime:        at com.android.internal.os.ZygoteInit.main(ZygoteInit.java:957)
03-31 09:13:22.740  1877  2919 W ActivityTaskManager:   Force finishing activity
com.motordsl.multaapp/crc6483bd6c7d9597e925.MainActivity
03-31 09:13:22.813  1877  2919 I ActivityManager: appDiedLocked: app=ProcessRecord{eccf6b3 10994:com.motordsl.multaapp/u0a356} 
thread=android.os.BinderProxy@9a7f800 fromBinderDied=true isKilledByAm=false reason=null
03-31 09:13:22.813  1877  2919 I ActivityManager: Process com.motordsl.multaapp (pid 10994) has died: fg  TOP
03-31 09:13:22.813  1877  2919 D RestartModeController: markWaitRestartAppIsSuicided null processName=com.motordsl.multaapp        
03-31 09:13:22.832  1877  1900 D CoreBackPreview: Window{a25e5a5 u0 Splash Screen com.motordsl.multaapp EXITING}: Setting back     
callback null
03-31 09:13:22.838  1877  2660 W InputManager-JNI: Input channel object 'a25e5a5 Splash Screen com.motordsl.multaapp (client)'     
was disposed without first being removed with the input manager!
03-31 09:13:23.243  1877  1915 W ActivityTaskManager: Activity top resumed state loss timeout for ActivityRecord{5fefc96 u0        
com.motordsl.multaapp/crc6483bd6c7d9597e925.MainActivity} t-1 f}}

## diagnostico de errores
PS E:\repos\tup\aplicada\2025\utn\ia\Ejemplos_Maui_PrintThermal_Driver> adb logcat -c
PS E:\repos\tup\aplicada\2025\utn\ia\Ejemplos_Maui_PrintThermal_Driver> adb shell am start -n com.motordsl.multaapp/crc6483bd6c7d9597e925.MainActivity
Starting: Intent { cmp=com.motordsl.multaapp/crc6483bd6c7d9597e925.MainActivity }
Warning: Activity not started, its current task has been brought to the front
PS E:\repos\tup\aplicada\2025\utn\ia\Ejemplos_Maui_PrintThermal_Driver> adb logcat | Select-String "motordsl|AndroidRuntime|FATAL|BT|Bluetooth|print|Print" | Select-Object -First 30

03-31 10:20:10.830  1877  2725 I ActivityTaskManager: START u0 {flg=0x10000000 
cmp=com.motordsl.multaapp/crc6483bd6c7d9597e925.MainActivity} from uid 2000
03-31 10:20:10.995 19480 19480 D nativeloader: Configuring clns-4 for other apk /system/framework/org.apache.http.legacy.jar. 
target_sdk_version=36, uses_libraries=ALL, library_path=/data/app/~~rc_MgbfzelyhmOIbtH18wA==/com.google.android.apps.photos-YEw-3u 
ScoP0IbmozjPYGpQ==/lib/arm64:/data/app/~~rc_MgbfzelyhmOIbtH18wA==/com.google.android.apps.photos-YEw-3uScoP0IbmozjPYGpQ==/base.apk 
!/lib/arm64-v8a:/data/app/~~rc_MgbfzelyhmOIbtH18wA==/com.google.android.apps.photos-YEw-3uScoP0IbmozjPYGpQ==/split_config.arm64_v8 
a.apk!/lib/arm64-v8a:/data/app/~~rc_MgbfzelyhmOIbtH18wA==/com.google.android.apps.photos-YEw-3uScoP0IbmozjPYGpQ==/split_config.es. 
apk!/lib/arm64-v8a:/data/app/~~rc_MgbfzelyhmOIbtH18wA==/com.google.android.apps.photos-YEw-3uScoP0IbmozjPYGpQ==/split_config.xxhdp 
i.apk!/lib/arm64-v8a, permitted_path=/data:/mnt/expand:/data/user/0/com.google.android.apps.photos
03-31 10:20:11.042  1877  1916 I LaunchCheckinHandler: MotoDisplayed
com.motordsl.multaapp/crc6483bd6c7d9597e925.MainActivity,wp,wa,212
03-31 10:20:11.074 19480 19480 D nativeloader: Configuring clns-5 for other apk /data/app/~~rc_MgbfzelyhmOIbtH18wA==/com.google.an
droid.apps.photos-YEw-3uScoP0IbmozjPYGpQ==/base.apk:/data/app/~~rc_MgbfzelyhmOIbtH18wA==/com.google.android.apps.photos-YEw-3uScoP 
0IbmozjPYGpQ==/split_config.arm64_v8a.apk:/data/app/~~rc_MgbfzelyhmOIbtH18wA==/com.google.android.apps.photos-YEw-3uScoP0IbmozjPYG 
pQ==/split_config.es.apk:/data/app/~~rc_MgbfzelyhmOIbtH18wA==/com.google.android.apps.photos-YEw-3uScoP0IbmozjPYGpQ==/split_config 
.xxhdpi.apk. target_sdk_version=36, uses_libraries=libOpenCL.so:libcdsprpc.so:libOpenCL.so, library_path=/data/app/~~rc_Mgbfzelyhm 
OIbtH18wA==/com.google.android.apps.photos-YEw-3uScoP0IbmozjPYGpQ==/lib/arm64:/data/app/~~rc_MgbfzelyhmOIbtH18wA==/com.google.andr 
oid.apps.photos-YEw-3uScoP0IbmozjPYGpQ==/base.apk!/lib/arm64-v8a:/data/app/~~rc_MgbfzelyhmOIbtH18wA==/com.google.android.apps.phot 
os-YEw-3uScoP0IbmozjPYGpQ==/split_config.arm64_v8a.apk!/lib/arm64-v8a:/data/app/~~rc_MgbfzelyhmOIbtH18wA==/com.google.android.apps 
.photos-YEw-3uScoP0IbmozjPYGpQ
03-31 10:20:11.089 30719 30719 I GoogleInputMethodService: GoogleInputMethodService.onStartInput():1434
onStartInput(EditorInfo{EditorInfo{packageName=com.motordsl.multaapp, inputType=0, inputTypeString=NULL, enableLearning=false,     
autoCorrection=false, autoComplete=false, imeOptions=0, privateImeOptions=null, actionName=UNSPECIFIED, actionLabel=null,
initialSelStart=-1, initialSelEnd=-1, initialCapsMode=0, label=null, fieldId=-1, fieldName=null, extras=null, hintText=null,       
hintLocales=[]}}, false)
03-31 10:20:11.439 19480 19521 D nativeloader: Load /data/app/~~rc_MgbfzelyhmOIbtH18wA==/com.google.android.apps.photos-YEw-3uScoP 
0IbmozjPYGpQ==/lib/arm64/libnative_crash_handler_jni.so using class loader ns clns-5
(caller=/data/app/~~rc_MgbfzelyhmOIbtH18wA==/com.google.android.apps.photos-YEw-3uScoP0IbmozjPYGpQ==/base.apk!classes2.dex): ok    
03-31 10:20:11.582  3238  3441 I BtGatt.ScanManager: msg.what = 6
03-31 10:20:11.582  3238  3441 D BtGatt.ScanManager: uid 10226 isForeground true scanMode -1
03-31 10:20:14.903 19480 19576 D nativeloader: Load
/data/app/~~rc_MgbfzelyhmOIbtH18wA==/com.google.android.apps.photos-YEw-3uScoP0IbmozjPYGpQ==/lib/arm64/libnative.so using class    
loader ns clns-5
(caller=/data/app/~~rc_MgbfzelyhmOIbtH18wA==/com.google.android.apps.photos-YEw-3uScoP0IbmozjPYGpQ==/base.apk!classes2.dex): ok


PS E:\repos\tup\aplicada\2025\utn\ia\Ejemplos_Maui_PrintThermal_Driver> adb logcat -c
PS E:\repos\tup\aplicada\2025\utn\ia\Ejemplos_Maui_PrintThermal_Driver> adb logcat | Select-String "Exception|Error|motordsl|Bluetooth|socket|connect" | Select-Object -First 40

03-31 10:20:54.230 22789 22789 W ThreadPoolForeg: type=1400 audit(0.0:45146): avc: denied { write } for name="traced_producer" 
dev="tmpfs" ino=23252 scontext=u:r:gmscore_app:s0:c512,c768 tcontext=u:object_r:traced_producer_socket:s0 tclass=sock_file
permissive=0 app=com.google.android.gms
03-31 10:20:54.237 27901 27901 W ThreadPoolForeg: type=1400 audit(0.0:45147): avc: denied { write } for name="traced_producer"     
dev="tmpfs" ino=23252 scontext=u:r:gmscore_app:s0:c512,c768 tcontext=u:object_r:traced_producer_socket:s0 tclass=sock_file
permissive=0 app=com.google.android.gms
03-31 10:20:57.219 15617 15723 E msys    : E[N WA]_WAJMNSStreamImplOnMNSFailure(287)=>[LogLevel:I]
WAJMNSStream/impl/callback/onMNSFailure/p=0xb4000073289fd680 (error=MNSTCPSocketReceive(): Received EOF)
03-31 10:20:57.222 15617 15723 E msys    : E[N WA]_WAJMNSStreamImplCreateConnectionReport(194)=>[LogLevel:I]
WAJMNSStream/impl/report port=443, connectedIPVersion=4, resolvedIPAddressCount=1, dnsResolutionStartTimeMS=142722425,
dnsResolutionEndTimeMS=142722428, dnsCacheHit=Yes, connectStartTimeMS=142722429, connectEndTimeMS=142722446,
proxyConnectStartTimeMS=0, proxyConnectEndTimeMS=0
03-31 10:20:57.222 15617 15723 E msys    : E[N WA]_WAJMNSStreamImplUpdateStreamState(230)=>[LogLevel:I]
WAJMNSStream/impl/state-changed/p=0xb4000073289fd680 (connected=>disconnected)
03-31 10:20:57.237 15617 15711 E msys    : E[N WA]WAJMNSStreamImplDisconnectSync(497)=>[LogLevel:W]
WAJMNSStream/impl/disconnect/p=0xb4000073289fd680 (bytesSent=11447)(bytesRecv=28732)
03-31 10:20:57.826  1014  1014 D vendor.qti.bluetooth@1.0-ibs_handler: SerialClockVote: vote for UART CLK ON
03-31 10:20:57.832  1014  1014 D vendor.qti.bluetooth@1.0-uart_transport: SocRxDWakeup: Flow off->Change UART baudrate to
38.4kbs->send 0x00->Change UART baudrate to max->Flow on
03-31 10:20:57.832  1014  1014 I vendor.qti.bluetooth@1.0-uart_transport: ## userial_vendor_ioctl: UART Flow Off
03-31 10:20:57.844  1014  1014 I vendor.qti.bluetooth@1.0-uart_transport: ## userial_vendor_set_baud: 17
03-31 10:20:57.855  1014  1014 I vendor.qti.bluetooth@1.0-uart_transport: SetBaudRate: in BOTHER
03-31 10:20:57.855  1014  1014 I vendor.qti.bluetooth@1.0-uart_transport: ## userial_vendor_ioctl: UART Flow On
03-31 10:20:57.856  1014  1014 D vendor.qti.bluetooth@1.0-wake_lock: Acquire wakelock is acquired 
03-31 10:20:57.856  1014  1014 I vendor.qti.bluetooth@1.0-ibs_handler: DeviceWakeUp: Writing IBS_WAKE_IND
03-31 10:20:57.857  1014  3504 I vendor.qti.bluetooth@1.0-ibs_handler: ProcessIbsCmd: Received IBS_WAKE_ACK: 0xFC
03-31 10:20:57.857  1014  3504 I vendor.qti.bluetooth@1.0-ibs_handler: ProcessIbsCmd: Signal wack_cond_
03-31 10:20:57.857  1014  1014 D vendor.qti.bluetooth@1.0-ibs_handler: DeviceWakeUp: Unblocked from waiting for FC,
pthread_cond_timedwait ret = 0
03-31 10:20:57.859  1014  3504 I vendor.qti.bluetooth@1.0-ibs_handler: ProcessIbsCmd: Received IBS_WAKE_IND: 0xFD
03-31 10:20:57.859  1014  3504 I vendor.qti.bluetooth@1.0-ibs_handler: ProcessIbsCmd: Writing IBS_WAKE_ACK
03-31 10:20:57.900  1014  3504 I vendor.qti.bluetooth@1.0-ibs_handler: ProcessIbsCmd: Received IBS_SLEEP_IND: 0xFE
03-31 10:20:58.116 19224 19398 I PlayCommon: [188] Connecting to server for timestamp: 
https://play.googleapis.com/play/log/timestamp
03-31 10:20:58.636 19224 19398 I PlayCommon: [188] Connecting to server: 
https://play.googleapis.com/play/log?format=raw&proto_v2=true
03-31 10:20:58.859  1014  3509 I vendor.qti.bluetooth@1.0-ibs_handler: DeviceSleep: TX Awake, Sending SLEEP_IND
03-31 10:20:58.859  1014  3509 D vendor.qti.bluetooth@1.0-ibs_handler: SerialClockVote: vote for UART CLK OFF
03-31 10:20:59.010  1014  3469 D vendor.qti.bluetooth@1.0-wake_lock: Release wakelock is released 


# ver logs
terminal 1:
adb logcat -c
adb logcat > C:\temp\multaapp.log

terminal 2:
Get-Content C:\temp\multaapp.log | Select-String "motordsl|Exception|Error|Bluetooth|socket|Print|failed" | Select-Object -First 50

## deploy

dotnet test PrintThermalDriver.sln --verbosity normal | Select-String "total|correcto|errores"

dotnet build -t:Run -f net10.0-android samples\MotorDsl.MultaApp\MotorDsl.MultaApp.csproj 2>&1 | Select-Object -Last 5

## lanzar una aplicacion con adb
adb shell am start -n com.motordsl.multaapp/crc6483bd6c7d9597e925.MainActivity

## compilar  y correr - sin el apk cacheado - tarda mas la recompilacion
dotnet build -t:Run -f net10.0-android samples\MotorDsl.MultaApp\MotorDsl.MultaApp.csproj --no-incremental