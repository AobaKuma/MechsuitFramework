
using Mono.Unix.Native;
using NAudio.Wave;
using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static HarmonyLib.Code;

namespace WalkerGear
{
    [StaticConstructorOnStartup]
    public class WalkerGear_Core : Apparel, IHealthParms
    {
        #region IHealthParms
        public float HPPercent => Health / HealthMax;

        public string PanelName => Wearer?.Name.ToStringShort + "\n" + "WG_StructurePoint".Translate();

        public string LabelHPPart => Health.ToString("F1");

        public string LabelMaxHPPart => HealthMax.ToString("F0");

        public string Tooltips => "WG_StructurePoint.Tooltip".Translate();

        public Texture2D FullShieldBarTex => fBarTex;
        public static readonly Texture2D fBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.24f));
        public Texture2D EmptyShieldBarTex => eBarTex;
        public static readonly Texture2D eBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);
        #endregion
        public float HealthMax => MaxHitPoints < combinedHealth ? combinedHealth : MaxHitPoints;
        public float Health
        {
            get
            {
                return healthInt == -1 ? healthInt = HealthMax : healthInt;
            }
            set
            {
                healthInt = value;
                if (healthInt < 0) healthInt = 0;
                if (healthInt > HealthMax) healthInt = HealthMax;
            }
        }
        public float HealthDamaged => HealthMax - Health;
        public BuildingWreckageExtension BuildingWreckage => def.GetModExtension<BuildingWreckageExtension>();

        public override IEnumerable<Gizmo> GetWornGizmos()
        {
            foreach (Gizmo gizmo in base.GetWornGizmos())
            {
                yield return gizmo;
            }
            yield return new Gizmo_HealthPanel(this);
        }
        public override void PreApplyDamage(ref DamageInfo dinfo, out bool absorbed)
        {
            DamageInfo _t = dinfo;
            _t.SetAmount(GetPostArmorDamage(ref dinfo));
            dinfo = _t;
            base.PreApplyDamage(ref dinfo, out absorbed);
        }
        public override bool CheckPreAbsorbDamage(DamageInfo dinfo)
        {
            if (!dinfo.Def.harmsHealth)
            {
                return true;
            }
            foreach (var a in Wearer.apparel.WornApparel)
            {
                if (a != this && a.TryGetComp(out CompShield c))
                {
                    //Log.Message("Shield: "+a.def);
                    c.PostPreApplyDamage(ref dinfo, out var absorbed);
                    if (absorbed) return true;
                }
            }
            if (Rand.Chance(0.1f))
            {
                Health -= dinfo.Amount / 2f; return true;
            }
            if (HPPercent < 0.5f && Rand.Chance(0.25f)) return false;

            float dmg = GetPostArmorDamage(ref dinfo);
            if (dmg <= 0)
            {
                EffecterDefOf.Deflect_Metal_Bullet.SpawnAttached(Wearer, Wearer.MapHeld, 1f);
            }
            else
            {
                EffecterDefOf.DamageDiminished_Metal.SpawnAttached(Wearer, Wearer.MapHeld, 1f);
                if (HPPercent < 0.5f && Rand.Chance(0.25f))
                {
                    FleckMaker.ThrowMicroSparks(Wearer.DrawPos, Wearer.Map);
                    GenPlace.TryPlaceThing(ThingMaker.MakeThing(RimWorld.ThingDefOf.Filth_Fuel), Wearer.Position, Wearer.Map, ThingPlaceMode.Near);
                }
            }
            Health -= dmg;
            if (Health <= 0)
            {
                GearDestory();
                return false;
            }
            return true;
        }
        public float GetPostArmorDamage(ref DamageInfo dinfo)
        {
            float amount = dinfo.Amount;
            DamageDef damageDef = dinfo.Def;
            // if (DebugSettings.godMode) Log.Message("Incoming DMG:" + amount);
            if (damageDef.armorCategory != null)
            {

                StatDef armorRatingStat = damageDef.armorCategory.armorRatingStat;
                //if (DebugSettings.godMode) Log.Message($"DMG Reduced:{armorRatingStat} Pen {dinfo.ArmorPenetrationInt}, Def {this.GetStatValue(armorRatingStat)}");
                ArmorUtility.ApplyArmor(ref amount, dinfo.ArmorPenetrationInt, this.GetStatValue(armorRatingStat), null, ref damageDef, Wearer, out _);
            }
            // if (DebugSettings.godMode) Log.Message("DMG Taken:" + amount);
            dinfo.SetAmount(amount);
            return amount;
        }
        public void CheckModules()
        {
            modules.Clear();
            foreach (Apparel a in Wearer.apparel.WornApparel)
            {
                if (a.HasComp<CompWalkerComponent>())
                {
                    modules.Add(a);
                }
            }
        }
        public bool RefreshHP(bool setup = false)
        {
            if (setup)
            {
                healthInt = -1f;
                combinedHealth = -1f;
                modules.Clear();
            }
            CheckModules();
            float hpmax = 0f;
            float hp = 0f;
            foreach (Thing t in modules)
            {
                if (t.TryGetQuality(out QualityCategory qc) && t.TryGetComp<CompWalkerComponent>(out var wc) &&
                    MechUtility.qualityToHPFactor.TryGetValue(qc, out float factor))
                {
                    hpmax += wc.MaxHP;
                    hp += wc.HP;
                    continue;
                }
                hpmax += 100;
                hp += 100;
            }
            combinedHealth = hpmax;
            if (setup) healthInt = hp;
            return true;
        }
        public void Eject()
        {
            //把殖民者彈出去，然後就地自毀。
            GenExplosion.DoExplosion(Wearer.Position, Wearer.Map, 5, DefDatabase<DamageDef>.GetNamed("Bomb"), null, 5);
            Building building;
            if (def.HasModExtension<BuildingWreckageExtension>())
            {
                building = ThingMaker.MakeThing(def.GetModExtension<BuildingWreckageExtension>().building) as Building;
            }
            else
            {
                building = (Building)ThingMaker.MakeThing(BuildingWreckage.building);
            }
            building.SetFactionDirect(Wearer.Faction);
            building.Rotation = Rot4.Random;

            GenPlace.TryPlaceThing(building, Wearer.Position, Wearer.Map, ThingPlaceMode.Direct);

            if (building is Building_Wreckage wreckage)
            {
                foreach (var m in modules)
                {
                    if (m == this) continue;
                    Wearer.apparel.Remove((Apparel)m);
                    m.HitPoints = 1;
                    if (Rand.Bool) wreckage.moduleContainer.Add(MechUtility.Conversion(m));
                }
                Pawn _p = Wearer;
                Wearer.apparel.Remove(this);
                MechUtility.WeaponDropCheck(_p);
            }
        }
        public void GearDestory()
        {
            GenExplosion.DoExplosion(Wearer.Position, Wearer.Map, 5, DefDatabase<DamageDef>.GetNamed("Bomb"), null, 5);
            Building building;
            if (def.HasModExtension<BuildingWreckageExtension>())
            {
                building = ThingMaker.MakeThing(def.GetModExtension<BuildingWreckageExtension>().building) as Building;
            }
            else
                building = (Building)ThingMaker.MakeThing(BuildingWreckage.building);
            building.SetFactionDirect(Wearer.Faction);
            building.Rotation = Rot4.Random;

            GenPlace.TryPlaceThing(building, Wearer.Position, Wearer.Map, ThingPlaceMode.Direct);

            if (building is Building_Wreckage wreckage)
            {
                Wearer.DeSpawnOrDeselect();
                wreckage.SetContained(Wearer);
                foreach (var m in modules)
                {
                    if (m == this) continue;
                    Wearer.apparel.Remove((Apparel)m);
                    m.HitPoints = 1;
                    if (Rand.Chance(0.25f)) wreckage.moduleContainer.Add(MechUtility.Conversion(m));
                }
                Pawn _p = Wearer;
                Wearer.apparel.Remove(this);
                MechUtility.WeaponDropCheck(_p);
            }
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref combinedHealth, "healthMax", -1f);
            Scribe_Values.Look(ref healthInt, "healthInt", -1f);
            Scribe_Values.Look(ref colorInt, "colorInt", Color.white);
            Scribe_Values.Look(ref totalCapacity, "totalCapacity", -1f);
            Scribe_Values.Look(ref capacity, "capacity", -1f);
            Scribe_Values.Look(ref deadWeight, "deadWeight", -1f);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                RefreshHP();
            }
        }

        public Color colorInt = Color.white;
        private float combinedHealth = -1;
        private float healthInt = -1;
        public List<Thing> modules = new();


        public bool Overload => this.capacity < this.deadWeight;
        public float TotalCapacity { get { return totalCapacity; } protected set { } }
        public float Capacity { get { return capacity; } protected set { } }
        public float DeadWeight { get { return deadWeight; } protected set { } }

        protected float totalCapacity;
        protected float capacity;
        protected float deadWeight;

        public void ResetStats(float totalCapacity, float capacity, float deadWeight)
        {
            this.deadWeight = deadWeight;
            this.totalCapacity = totalCapacity;
            this.capacity = capacity;
        }
    }
}
