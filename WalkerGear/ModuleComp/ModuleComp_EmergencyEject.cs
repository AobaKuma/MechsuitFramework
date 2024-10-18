using RimWorld;
using System.Collections.Generic;
using Verse;

namespace WalkerGear
{
    public class ModuleComp_EmergencyEject : ModuleComp
    {
        public bool safetyDisabled = false;
        public override IEnumerable<Gizmo> CompGetWornGizmosExtra()
        {
            Command_Toggle toggle = new Command_Toggle
            {
                Order = -999f,
                Disabled = !Wearer.IsPlayerControlled,
                icon = safetyDisabled ? Resources.GetSafetyIcon_Disabled : Resources.GetSafetyIcon,
                defaultLabel = "WG_SafetyLock".Translate(),
                defaultDesc = "WG_SafetyLock_Desc".Translate(),
                isActive = () => safetyDisabled,
                toggleAction = delegate
                {
                    safetyDisabled = !safetyDisabled;
                }
            };
            yield return toggle;

            if (toggle.isActive())
            {
                Command_Action command = new Command_Action
                {
                    Order = -998f,
                    defaultLabel = "WG_EmergencyEject".Translate(),
                    defaultDesc = "WG_EmergencyEject_Desc".Translate(),
                    icon = Resources.GetOutIcon,
                    action = delegate
                    {
                        Core.Eject();
                    }
                };
                if ((!Wearer.IsPlayerControlled))
                {
                    command.Disable("WG_Disabled_NeedControlledAndDrafted".Translate());
                }
                yield return command;
            }
        }
    }
}