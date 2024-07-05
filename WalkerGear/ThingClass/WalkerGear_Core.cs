
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace WalkerGear
{
    [StaticConstructorOnStartup]
	public class WalkerGear_Core : Apparel,IHealthParms
    {
        #region IHealthParms
        public float HPPercent => Health / HealthMax;

        public string PanelName => "StructurePoint".Translate();

        public string LabelHPPart => Health.ToString("F1");

        public string LabelMaxHPPart => HealthMax.ToString("F0");

        public string Tooltips => "StructurePoint.Tooltip".Translate();

        public Texture2D FullShieldBarTex => fBarTex;
        public static readonly Texture2D fBarTex = SolidColorMaterials.NewSolidColorTexture(new Color(0.2f, 0.2f, 0.24f));
        public Texture2D EmptyShieldBarTex => eBarTex;
        public static readonly Texture2D eBarTex = SolidColorMaterials.NewSolidColorTexture(Color.clear);
        #endregion
        public float HealthMax => MaxHitPoints < combinedHealth?combinedHealth: MaxHitPoints;
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
		public float HealthDamaged=>HealthMax-Health;
        
        public BuildingWreckage BuildingWreckage=>def.GetModExtension<BuildingWreckage>();
        public override IEnumerable<Gizmo> GetWornGizmos()
		{
			foreach(Gizmo gizmo in base.GetWornGizmos())
			{
				yield return gizmo;
			}
			yield return new Gizmo_HealthPanel(this);
		}
		public override bool CheckPreAbsorbDamage(DamageInfo dinfo)
		{
            if (!dinfo.Def.harmsHealth)
            {
                return true;
            }
            float dmg = GetPostArmorDamage(dinfo);
            foreach (var a in Wearer.apparel.WornApparel)
            {

                if (a!= this && a.TryGetComp(out CompShield c))
                {
                    //Log.Message("Shield: "+a.def);
                    c.PostPreApplyDamage(ref dinfo, out var absorbed);
                    if (absorbed) return true;
                }
            }
			
            if (Health <= 0)
            {
                return false;
            }

            Health -= dmg;
            if (Health <= 0)
            {
                GearDestory();
                return false;
            }
            return true;
		}
		public float GetPostArmorDamage(DamageInfo dinfo)
		{
            float amount = dinfo.Amount;
            DamageDef damageDef = dinfo.Def;
            if (DebugSettings.godMode)
            {
                Log.Message("Incoming DMG:"+amount);
            }
            if (damageDef.armorCategory != null)
            {
                
                StatDef armorRatingStat = damageDef.armorCategory.armorRatingStat;
                if (DebugSettings.godMode) Log.Message($"DMG Reduced:{armorRatingStat} Pen {dinfo.ArmorPenetrationInt}, Def {this.GetStatValue(armorRatingStat)}");
                ArmorUtility.ApplyArmor(ref amount, dinfo.ArmorPenetrationInt, this.GetStatValue(armorRatingStat), null, ref damageDef, Wearer, out _);
            }
            if (DebugSettings.godMode)
            {
                Log.Message("DMG Taken:" + amount);
            }
            dinfo.SetAmount(amount);
            return amount;
        }
		public bool RefreshHP(bool setup=false)
		{
            if(setup)
            {
                healthInt = -1f;
                combinedHealth = -1f;
                modules.Clear();
            }
			CheckModules();
			float hpmax=0f;
            float hp=0f;
            foreach (var t in modules)
            {
                if (t.TryGetQuality(out QualityCategory qc) && t.TryGetComp<CompWalkerComponent>(out var wc)&&
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
		public void CheckModules()
		{
			modules.Clear();
			foreach(Apparel a in Wearer.apparel.WornApparel)
			{
				if (a.HasComp<CompWalkerComponent>())
				{
					modules.Add(a);
                }
			}
		}
		public void GearDestory()
		{
            GenExplosion.DoExplosion(Wearer.Position, Wearer.Map, 5, DamageDefOf.Bomb, null, 5);
            Building building;
            if (def.HasModExtension<BuildingWreckage>())
            {
                building = ThingMaker.MakeThing(def.GetModExtension<BuildingWreckage>().building) as Building;
            }
            else
                building = (Building)ThingMaker.MakeThing(BuildingWreckage.building);
            building.SetFactionDirect(Wearer.Faction);
            building.Rotation = Rot4.South;
			
            GenPlace.TryPlaceThing(building, Wearer.Position, Wearer.Map, ThingPlaceMode.Direct);
            
            if (building is Building_Wreckage wreckage)
            {
                Wearer.DeSpawnOrDeselect();
                wreckage.pawnContainer.Add(Wearer);
                foreach (var m in modules)
                {
                    if (m==this) continue;
                    Wearer.apparel.Remove((Apparel)m);
                    m.HitPoints = 1;
                    if (Rand.Bool) wreckage.moduleContainer.Add(MechUtility.Conversion(m));
                }
                Wearer.apparel.Remove(this);
            }
			
            
        }
		public override void ExposeData()
		{
			base.ExposeData();
            Scribe_Values.Look(ref combinedHealth, "healthMax", -1f);
			Scribe_Values.Look(ref healthInt, "healthInt",-1f);
			Scribe_Values.Look(ref colorInt, "colorInt",Color.white);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                RefreshHP();
            }
		}
        

        public Color colorInt = Color.white;
		private float combinedHealth=-1;
		private float healthInt = -1;
		public List<Thing> modules = new();
        
        
    }

    public class BuildingWreckage : DefModExtension
    {
        public ThingDef building;
    }
}

