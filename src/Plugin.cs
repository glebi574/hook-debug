using BepInEx;
using BepInEx.Logging;
using MonoMod.RuntimeDetour;
using MonoMod.RuntimeDetour.HookGen;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;

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

    // Idk why people would have it installed anywhere under the Users folder, but just to be safe...
    private static string DoNotDoxxPeoplePlease(string path) => Regex.Replace(path, @"[/\\]Users[/\\][^/\\]+[/\\]", "\\Users\\USER\\");

    private void ProcessManager_ActualProcessSwitch(On.ProcessManager.orig_ActualProcessSwitch orig, ProcessManager self, ProcessManager.ProcessID ID, float fadeOutSeconds)
    {
        orig(self, ID, fadeOutSeconds);

        var filePath = Path.Combine(Custom.LegacyRootFolderDirectory(), "HookDebug.txt");
        try
        {
            // Grab all hooks
            Dictionary<MethodBase, Dictionary<Delegate, Stack<IDetour>>> hookMap = [];
            Dictionary<MethodBase, object> hookEndpointMap = (Dictionary<MethodBase, object>)typeof(HookEndpointManager).GetField("HookEndpointMap", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
            foreach (var kvp in hookEndpointMap)
            {
                hookMap.Add(kvp.Key, ModManager.GetHookMap(kvp.Key));
            }

            // Grab all assemblies
            bool successfulLoadedAssemblies = true;
            Dictionary<string, string> loadedAssemblyStrings = [];
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var ass in assemblies)
            {
                try
                {
                    loadedAssemblyStrings.Add(ass.GetName().Name, $"{ass.FullName}, Location={DoNotDoxxPeoplePlease(ass.Location)}");
                }
                catch (Exception ex)
                {
                    successfulLoadedAssemblies = false;
                    Logger.LogError(ex);
                }
            }

            // Order the thingies
            var sortedList = hookMap.OrderBy(x => x.Key.DeclaringType.Namespace).ThenBy(x => x.Key.DeclaringType.Name).ThenBy(x => x.Key.Name).ToList(); // scary type (you do not want to know)
            List<string> sortedAsses = loadedAssemblyStrings.OrderBy(x => x.Key).Select(x => x.Value).ToList(); // not scary type

            // Generate the output string
            var sb = new StringBuilder();
            sb.AppendLine($"Generated at {DateTime.Now.ToUniversalTime():yyyy-MM-dd HH:mm:ss} UTC\n");

            foreach (var kvp in sortedList)
            {
                sb.AppendLine($"{kvp.Key.DeclaringType.FullName}.{kvp.Key.Name}");
                foreach (var hook in kvp.Value)
                {
                    sb.AppendLine($"    ({string.Join(", ", hook.Value.Select(x => x.GetType().Name))}) {hook.Key.Method.DeclaringType.FullName}.{hook.Key.Method.Name}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("\nLOADED ASSEMBLIES:");
            if (!successfulLoadedAssemblies)
                sb.AppendLine("NOTE: Errored loading some assemblies! See BepInEx/LogOutput.log for more details. Errored assemblies are skipped in this list.");
            foreach (var ass in sortedAsses)
            {
                sb.Append("    ");
                sb.AppendLine(ass);
            }

            File.WriteAllText(filePath, sb.ToString());
        }
        catch (Exception e)
        {
            Logger.LogError(e);
            try
            {
                File.WriteAllText(filePath, e.ToString());
            }
            catch { }
        }
    }
}
