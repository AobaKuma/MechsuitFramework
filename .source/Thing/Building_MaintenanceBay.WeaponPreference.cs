using System.Collections.Generic;
using System.Linq;
using Multiplayer.API;
using RimWorld;
using Verse;

namespace Exosuit
{
    // 上机默认武器偏好
    public partial class Building_MaintenanceBay
    {
        #region 字段

        // 优先模块武器
        protected bool preferModuleWeapon = false;
        // true=同时存在时优先近战
        protected bool preferMeleeModuleWeapon = false;

        #endregion

        #region 公共方法

        // 取所有可用模块武器
        public IEnumerable<CompModuleWeapon> EnumerateModuleWeapons()
        {
            if (Dummy?.apparel == null) yield break;
            foreach (Apparel a in Dummy.apparel.WornApparel)
            {
                if (a.TryGetComp<CompModuleWeapon>(out var c) && c.Weapon != null)
                {
                    yield return c;
                }
            }
        }

        public bool HasAnyModuleWeapon => EnumerateModuleWeapons().Any();
        public bool HasMeleeModuleWeapon => EnumerateModuleWeapons().Any(c => c.Weapon.def.IsMeleeWeapon);
        public bool HasRangedModuleWeapon => EnumerateModuleWeapons().Any(c => c.Weapon.def.IsRangedWeapon);

        // 取偏好类型的模块武器
        public CompModuleWeapon GetPreferredModuleWeapon()
        {
            var list = EnumerateModuleWeapons().ToList();
            if (list.Count == 0) return null;

            bool hasMelee = list.Any(c => c.Weapon.def.IsMeleeWeapon);
            bool hasRanged = list.Any(c => c.Weapon.def.IsRangedWeapon);
            if (hasMelee && hasRanged)
            {
                CompModuleWeapon match = preferMeleeModuleWeapon
                    ? list.FirstOrDefault(c => c.Weapon.def.IsMeleeWeapon)
                    : list.FirstOrDefault(c => c.Weapon.def.IsRangedWeapon);
                if (match != null) return match;
            }
            return list[0];
        }

        // 上机后应用武器偏好
        public void ApplyWeaponPreferenceOnGearUp(Pawn pawn)
        {
            if (!preferModuleWeapon) return;
            if (pawn.equipment == null) return;

            CompModuleWeapon target = null;
            foreach (Apparel a in pawn.apparel.WornApparel)
            {
                if (a.TryGetComp<CompModuleWeapon>(out var c) && c.Weapon != null)
                {
                    bool isMelee = c.Weapon.def.IsMeleeWeapon;
                    bool isRanged = c.Weapon.def.IsRangedWeapon;
                    if (target == null) { target = c; }
                    else
                    {
                        bool curMelee = target.Weapon.def.IsMeleeWeapon;
                        if (preferMeleeModuleWeapon && isMelee && !curMelee) target = c;
                        else if (!preferMeleeModuleWeapon && isRanged && !target.Weapon.def.IsRangedWeapon) target = c;
                    }
                }
            }
            if (target == null) return;

            ThingWithComps weapon = target.Weapon;
            if (weapon == null) return;
            if (pawn.equipment.Primary == weapon) return;

            // 把原武器塞进背包
            if (pawn.equipment.Primary != null)
            {
                pawn.equipment.TryTransferEquipmentToContainer(pawn.equipment.Primary, pawn.inventory.innerContainer);
            }
            if (weapon.holdingOwner == null)
            {
                weapon.DeSpawnOrDeselect();
            }
            weapon.holdingOwner?.Remove(weapon);
            ThingOwner_TryTransferToContainer.thingListening = null;
            pawn.equipment.AddEquipment(weapon);
        }

        #endregion
    }
}