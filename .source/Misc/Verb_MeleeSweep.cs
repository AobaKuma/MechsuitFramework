using Verse;
using RimWorld;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Exosuit
{
    public class Verb_MeleeSweep : Verb_MeleeAttack
    {
        private EffecterDef ExplosionEffect => EquipmentSource.GetComp<Comp_MeleeSweep>().Props.explosionEffect;
        private DamageDef DamageDef => EquipmentSource.GetComp<Comp_MeleeSweep>().Props.damageDef;
        private float Radius => EquipmentSource.GetComp<Comp_MeleeSweep>().Props.radius;
        private float Angle => EquipmentSource.GetComp<Comp_MeleeSweep>().Props.angle;
        private float Damage => EquipmentSource.GetComp<Comp_MeleeSweep>().Props.damage;
        private float ArmorPenetration => EquipmentSource.GetComp<Comp_MeleeSweep>().Props.armorPenetration;
        public override bool TryStartCastOn(LocalTargetInfo castTarg, LocalTargetInfo destTarg, bool surpriseAttack = false, bool canHitNonTargetPawns = true, bool preventFriendlyFire = false, bool nonInterruptingSelfCast = false)
        {
            return base.TryStartCastOn(castTarg, destTarg, true, canHitNonTargetPawns, preventFriendlyFire, nonInterruptingSelfCast);
        }
        public override DamageWorker.DamageResult ApplyMeleeDamageToTarget(LocalTargetInfo target)
        {
            DamageWorker.DamageResult result = new DamageWorker.DamageResult();
            Pawn casterPawn = CasterPawn;
            if (casterPawn != null && !casterPawn.Downed && CasterIsPawn)
            {
                DoSweep(target, out result);
            }
            return result;
        }
        public override bool IsMeleeAttack => true;
        public void DoSweep(LocalTargetInfo target, out DamageWorker.DamageResult result)
        {
            if (ExplosionEffect != null)
            {
                Effecter effecter = ExplosionEffect.Spawn();
                effecter.Trigger(new TargetInfo(target.Cell, target.Thing.Map), new TargetInfo(target.Thing.PositionHeld, target.Thing.Map));
                effecter.Cleanup();
            }

            IntVec3 pos = CasterPawn.Position;
            float direction = Mathf.Atan2(-(target.Cell.z - pos.z), target.Cell.x - pos.x) * 57.29578f;

            target.Thing.TakeDamage(new DamageInfo(DamageDefOf.Stun, 20f * Rand.Range(1f, 3f)));

            //武器品質
            var q = QualityCategory.Normal;
            EquipmentSource.TryGetQuality(out q);

            result = target.Thing.TakeDamage(new DamageInfo(DamageDef, Damage, armorPenetration: ArmorPenetration, weapon: EquipmentSource.def, instigator: CasterPawn, weaponQuality: q));
            if (EquipmentSource != null && !EquipmentSource.Destroyed)
            {
                //友傷判斷
                List<Thing> AvoidThings = new List<Thing>();
                AvoidThings.Add(CasterPawn);
                foreach (IntVec3 cell in CasterPawn.OccupiedRect().ExpandedBy((int)Radius))
                {
                    List<Thing> list = CasterPawn.MapHeld.
                        thingGrid.ThingsListAt(cell).Where((v) => 
                        (v is Pawn pawn && (pawn.DeadOrDowned || pawn.IsPlayerControlled)) 
                        || 
                        (v.def.category == ThingCategory.Item)
                        ).ToList();
                    if (!list.NullOrEmpty()) AvoidThings.AddRange(list);
                }

                if (target.Pawn == null)
                {
                    pos += Rot4.FromAngleFlat(direction).Opposite.FacingCell; //面對非Pawn目標的範圍會更大。
                }
                GenExplosion.DoExplosion(pos, CasterPawn.Map, Radius, DamageDef, CasterPawn, (int)Damage, armorPenetration: ArmorPenetration, screenShakeFactor: 0, doVisualEffects: false, ignoredThings: AvoidThings, explosionSound: SoundDefOf.Pawn_Melee_Punch_Miss, affectedAngle: new FloatRange(direction - (Angle / 2f)-10, direction + (Angle / 2f)+10), direction: direction, propagationSpeed: 0.6f);
            }
        }
    }
    public class CompProperties_MeleeSweep : CompProperties
    {
        public DamageDef damageDef;

        public float radius = 3f;
        public float angle = 90f;
        public float damage = 50f;

        public float armorPenetration = 0.5f;

        public EffecterDef explosionEffect;

        public CompProperties_MeleeSweep()
        {
            compClass = typeof(Comp_MeleeSweep);
        }
    }
    public class Comp_MeleeSweep : ThingComp
    {
        public CompProperties_MeleeSweep Props => (CompProperties_MeleeSweep)props;
    }
}
