// 当白昼倾坠之时
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using System.Linq;
using System;
using Verse.AI;
using Exosuit.HarmonyPatches;

namespace Exosuit
{
    [StaticConstructorOnStartup]
    public class Exosuit_Core : Apparel, IHealthParms
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
                if(needHPRecache)
                {
                    RefreshHP();
                }
                return healthInt == -1 ? healthInt = HealthMax : healthInt;
            }
            set
            {
                value = Mathf.Clamp(value, 0, HealthMax);
                if (healthInt!=value)
                {
                    var dif = healthInt - value;
                    healthInt = value;
                    OnHealthChanged(dif);
                }
                
            }
        }
        public float HealthDamaged => HealthMax - Health;
        public bool Damaged=> HealthMax > Health;

        public ExosuitExt Extesnsion=> ext??=def.GetModExtension<ExosuitExt>();
        private ExosuitExt ext;
        public float ArmorBreakdownThreshold => Extesnsion?.minArmorBreakdownThreshold ?? 0.25f;
        public override IEnumerable<Gizmo> GetWornGizmos()
        {
            foreach (Gizmo gizmo in base.GetWornGizmos())
            {
                yield return gizmo;
            }
            yield return new Gizmo_HealthPanel(this);
        }
        // 判定伤害吸收逻辑
        public override bool CheckPreAbsorbDamage(DamageInfo dinfo)
        {
            if (!dinfo.Def.harmsHealth)
            {
                return true;
            }
            // 优先处理护盾判定
            foreach (var a in Wearer.apparel.WornApparel)
            {
                if (a != this && a.TryGetComp(out CompShield c))
                {
                    c.PostPreApplyDamage(ref dinfo, out var absorbed);
                    if (absorbed) return true;
                }
            }
            // 低耐久触发穿透判定
            if (HPPercent < ArmorBreakdownThreshold && Rand.Chance(0.25f)) return false;

            // 计算护甲减伤
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
            // 判定机体毁坏状态
            Health -= dmg;
            // 防止爆炸伤害溢出
            return true;
        }
        
        public virtual float GetPostArmorDamage(ref DamageInfo dinfo)
        {
            float amount = dinfo.Amount;
            DamageDef damageDef = dinfo.Def;
            if (damageDef.armorCategory != null)
            {
                StatDef armorRatingStat = damageDef.armorCategory.armorRatingStat;

                // 判定穿甲击穿效果
                if (Rand.Chance(0.25f) && dinfo.ArmorPenetrationInt * 2 > this.GetStatValue(armorRatingStat))
                {
                    ArmorUtility.ApplyArmor(ref amount, dinfo.ArmorPenetrationInt, this.GetStatValue(armorRatingStat)/2, null, ref damageDef, Wearer, out _);
                }
                else
                {
                    ArmorUtility.ApplyArmor(ref amount, dinfo.ArmorPenetrationInt, this.GetStatValue(armorRatingStat), null, ref damageDef, Wearer, out _);
                }
            }
            dinfo.SetAmount(amount);
            return amount;
        }
        
        // 重算模组缓存数据
        public void ModuleRecache()
        {
            modules.Clear();
            modules.AddRangeWhereFast(Wearer.apparel?.WornApparel, a=>a.HasComp<CompSuitModule>());

            RefreshHP();
            RefreshCapacity();
        }
        public bool RefreshHP()
        {
            healthInt = 0f;
            combinedHealth = 0f;
            foreach (Thing t in modules)
            {
                if (t.TryGetComp<CompSuitModule>(out var wc))
                {
                    combinedHealth += wc.MaxHP;
                    healthInt += wc.HP;
                }
            }
            return true;
        }
        public virtual void OnHealthChanged(float amount)
        {
            if (Health == 0)
            {
                ExosuitDestory();
                return;
            }
            if (amount > 0)
            {
                LongEventHandler.ExecuteWhenFinished(()=>ApplyDamageToModules(amount));
            }
        }
        protected virtual void ApplyDamageToModules(float amount)
        {
            List<CompSuitModule> _moduleComps = [];
            foreach (var module in modules)
            {
                if (module.TryGetComp<CompSuitModule>(out var c) && c.HP>1)
                {
                    _moduleComps.Add(c);
                }
            }
            float _tmp=amount;
            CompSuitModule _comp;
            while (amount > 0)
            {
                if (_moduleComps.Count == 0)
                {
                    Log.Warning("Unable To Apply Damage To Module");
                    break;
                }
                _comp = _moduleComps.RandomElement();
                amount-=Mathf.Min(_comp.HP - 1, amount);
                _comp.HP-=(int)(_tmp-amount);
                if (_comp.HP <= 1)
                {
                    _moduleComps.Remove(_comp);
                }
            }
        }
        //默认爆炸
        public virtual void PreDestroy(){
            GenExplosion.DoExplosion(Wearer.Position, Wearer.Map, 5, DefDatabase<DamageDef>.GetNamed("Bomb"), null, 5);
        }
        // 执行机兵毁坏逻辑
        public void ExosuitDestory()
        {
            PreDestroy();

            Thing building = ThingMaker.MakeThing(Extesnsion?.wreckageOverride ?? ThingDefOf.MF_Building_Wreckage);
            if (building is not Building_Wreckage wreckage)
            {
                Log.Warning($"ThingDef: {Extesnsion?.wreckageOverride} is not an acceptable type of Building_Wreckage, fallback to default");
                wreckage = (Building_Wreckage)ThingMaker.MakeThing(ThingDefOf.MF_Building_Wreckage);
            }


            wreckage.SetFactionDirect(Wearer.Faction);
            wreckage.Rotation = Rot4.Random;

            GenPlace.TryPlaceThing(wreckage, Wearer.Position, Wearer.Map, ThingPlaceMode.Near);
            Wearer.DeSpawnOrDeselect();
            wreckage.SetContained(Wearer);
            foreach (var m in modules)
            {
                if (m == this) continue;
                var a = (Apparel)m;

                // 处理组件毁坏逻辑
                foreach (var comp in a.AllComps)
                {
                    if (comp is IExosuitDestructionHandler handler)
                    {
                        handler.OnExosuitDestroyed(wreckage);
                    }
                }

                Wearer.apparel.Remove(a);
                m.HitPoints = 1;
                if (Rand.Chance(0.25f)) wreckage.moduleContainer.Add(MechUtility.Conversion(m));
            }
            Pawn _p = Wearer;
            //最后移除自己
            Wearer.apparel.Remove(this);
            MechUtility.WeaponDropCheck(_p);
            
        }
        
        public virtual void PostDestroy(){ }
        
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref combinedHealth, "healthMax", -1f);
            Scribe_Values.Look(ref healthInt, "healthInt", -1f);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                ModuleRecache();
            }
        }

        private float combinedHealth = -1;
        private float healthInt = -1;
        public List<Thing> modules = [];

        protected bool needHPRecache = false;
        public void SetHPDirty() {
            if(!needHPRecache)
            {
                needHPRecache = true;
            }
        }
        public bool Overload => Capacity < DeadWeight;
        public float Capacity { get; protected set; }
        public float DeadWeight { get; protected set; }

        public void RefreshCapacity()
        {
            DeadWeight=0f;
            Capacity=0f;
            modules.ForEach(m => {
                if (m.IsModule())
                {
                    Capacity += m.def.equippedStatOffsets.GetStatOffsetFromList(StatDefOf.CarryingCapacity);
                    DeadWeight += m.GetStatValue(StatDefOf.Mass);
                }
            });
        }
    
    }
}
