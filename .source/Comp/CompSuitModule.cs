﻿using RimWorld;
using RimWorld.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;


namespace Exosuit
{
    public class CompSuitModule : ThingComp, IReloadableComp
    {
        public CompProperties_ExosuitModule Props
        {
            get
            {
                return props as CompProperties_ExosuitModule;
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref remainingCharges, "wc_remainingCharges", -999);
            Scribe_Values.Look(ref hp, "wc_hp", -1);
            if (Scribe.mode == LoadSaveMode.PostLoadInit && remainingCharges == -999)
            {
                remainingCharges = 0;
            }
        }
        //public bool NeedMaintenance => NeedAmmo || NeedRepair;
        public bool NeedMaintenance => NeedRepair;
        public bool NeedAmmo => hasReloadableProps && remainingCharges < MaxCharges;
        public bool NeedRepair => parent.HitPoints < parent.MaxHitPoints;
        public List<SlotDef> Slots => Props.occupiedSlots;

        public ThingDef AmmoDef => ammoDef;
        public int MaxCharges => maxCharges;
        public int RemainingCharges
        {
            get => remainingCharges;
            set => remainingCharges = value;
        }
        public int NeedAmmoCount => (MaxCharges - RemainingCharges) * ammoCountPerCharge;
        public Thing ReloadableThing => parent;
        public int BaseReloadTicks => baseReloadTicks;
        public string LabelRemaining => string.Format("{0} / {1}", RemainingCharges, MaxCharges);
        public bool NeedsReload(bool allowForceReload)
        {
            if (ammoDef == null)
            {
                return false;
            }
            if (ammoCountToRefill == 0)
            {
                return RemainingCharges != MaxCharges;
            }
            if (!allowForceReload)
            {
                return RemainingCharges == 0;
            }
            return RemainingCharges != MaxCharges;
        }
        public int MinAmmoNeeded(bool allowForcedReload)
        {
            if (!NeedsReload(allowForcedReload))
            {
                return 0;
            }
            if (ammoCountToRefill != 0)
            {
                return ammoCountToRefill;
            }
            return ammoCountPerCharge;
        }

        public int MaxAmmoNeeded(bool allowForcedReload)
        {
            if (!NeedsReload(allowForcedReload))
            {
                return 0;
            }
            if (ammoCountToRefill != 0)
            {
                return ammoCountToRefill;
            }
            return ammoCountPerCharge * (MaxCharges - RemainingCharges);
        }
        public int MaxAmmoAmount()
        {
            if (ammoDef == null)
            {
                return 0;
            }
            if (ammoCountToRefill == 0)
            {
                return ammoCountPerCharge * MaxCharges;
            }
            return ammoCountToRefill;
        }

        public void ReloadFrom(Thing ammo)
        {
            if (!NeedsReload(true))
            {
                return;
            }
            if (ammoCountToRefill != 0)
            {
                if (ammo.stackCount < ammoCountToRefill)
                {
                    return;
                }
                ammo.SplitOff(ammoCountToRefill).Destroy(DestroyMode.Vanish);
                RemainingCharges = MaxCharges;
            }
            else
            {
                if (ammo.stackCount < ammoCountPerCharge)
                {
                    return;
                }
                int num = Mathf.Clamp(ammo.stackCount / ammoCountPerCharge, 0, MaxCharges - RemainingCharges);
                ammo.SplitOff(num * ammoCountPerCharge).Destroy(DestroyMode.Vanish);
                RemainingCharges += num;
            }
            soundReload?.PlayOneShot(new TargetInfo(parent.PositionHeld, parent.MapHeld, false));
        }

        public string DisabledReason(int minNeeded, int maxNeeded) => "";

        public bool CanBeUsed(out string reason) { reason = ""; return false; }
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!parent.def.IsApparel && Props.EquipedThingDef != null)
            {
                var def = Props.EquipedThingDef;
                var cp = def.GetCompProperties<CompProperties_ApparelReloadable>();
                if (cp != null)
                {
                    hasReloadableProps = true;
                    ammoDef = cp.ammoDef;
                    ammoCountToRefill = cp.ammoCountToRefill;
                    ammoCountPerCharge = cp.ammoCountPerCharge;
                    baseReloadTicks = cp.baseReloadTicks;
                    replenishAfterCooldown = cp.replenishAfterCooldown;
                    soundReload = cp.soundReload;
                    maxCharges = cp.maxCharges;
                }
            }
        }
        public int HP
        {
            get
            {
                if (parent is Apparel)
                {
                    return hp;
                }
                return parent.HitPoints;
            }
            set
            {
                if (parent is Apparel)
                {
                    hp = value;
                    return;
                }
                parent.HitPoints = value;
            }
        }
        public int MaxHP
        {
            get
            {
                if (maxhp < 0)
                {
                    float m = parent is Apparel ? Props.ItemDef.BaseMaxHitPoints : parent.MaxHitPoints;
                    if (parent.TryGetQuality(out var qc))
                        m *= MechUtility.qualityToHPFactor[qc];
                    maxhp = Mathf.FloorToInt(m);
                }

                return maxhp;
            }
        }

        public bool hasReloadableProps;
        public ThingDef ammoDef;
        public int ammoCountToRefill;
        public int ammoCountPerCharge;
        public int baseReloadTicks = 60;
        public bool replenishAfterCooldown;
        public SoundDef soundReload;
        public int remainingCharges;
        public int maxCharges;
        private int hp = -1;
        private int maxhp = -1;

        public override string CompInspectStringExtra()
        {
            string s = base.CompInspectStringExtra();
            if (hasReloadableProps)
            {
                s += LabelRemaining;
            }
            return s;
        }
    }
    public class CompProperties_ExosuitModule : CompProperties
    {
        public CompProperties_ExosuitModule()
        {
            compClass = typeof(CompSuitModule);
        }
        public ThingDef EquipedThingDef;//提供的裝備
        public ThingDef ItemDef;//物品def
        public List<SlotDef> occupiedSlots;//占用的格子
        public List<SlotDef> disabledSlots;//核心禁用的格子
        public float repairEfficiency = 0.01f;//作為物品被修理的效率
        public override IEnumerable<string> ConfigErrors(ThingDef parentDef)
        {
            if (occupiedSlots.NullOrEmpty())
            {
                return base.ConfigErrors(parentDef).Append("No proper slot");
            }
            if (occupiedSlots.GroupBy(slot => slot.uiPriority).Any(g => g.Count() > 1))
            {
                return base.ConfigErrors(parentDef).Append("Defined occupiedSlots are using duplicated uiPriority");
            }
            return base.ConfigErrors(parentDef);
        }
    }
}
