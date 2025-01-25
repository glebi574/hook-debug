using BepInEx;
using BepInEx.Logging;
using System.Security.Permissions;

// Allows access to private members
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace HookDebug;

[BepInPlugin("alduris.hookdebug", "Hook Debug", "1.0.0")]
sealed class Plugin : BaseUnityPlugin
{
    public static new ManualLogSource Logger;

    public void OnEnable()
    {
        Logger = base.Logger;
        On.ProcessManager.ActualProcessSwitch += ProcessManager_ActualProcessSwitch;
    }

    private void ProcessManager_ActualProcessSwitch(On.ProcessManager.orig_ActualProcessSwitch orig, ProcessManager self, ProcessManager.ProcessID ID, float fadeOutSeconds)
    {
        orig(self, ID, fadeOutSeconds);

        try
        {
            //
        }
        catch { }
    }
}
