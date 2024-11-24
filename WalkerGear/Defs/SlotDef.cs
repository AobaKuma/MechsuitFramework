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
    //[StaticConstructorOnStartup]
    public class SlotDef : Def
    {
        public bool isCoreFrame;//用來顯示的CoreFrame只會有一個
        public bool isWeapon;
        public List<SlotDef> supportedSlots;//填入組裝塢後提供的新槽位
        public int uiPriority; //slot在UI里占用的格子
        public override IEnumerable<StatDrawEntry> SpecialDisplayStats(StatRequest req)
        {
            foreach (StatDrawEntry item in base.SpecialDisplayStats(req))
            {
                yield return item;
            }

            var user = ModuleUtil.SlotUsers(this);
            if (!user.NullOrEmpty())
            {
                yield return new StatDrawEntry(StatCategoryDefOf.BasicsImportant, StatDefof.MF_Stat_Slot.label,this.label, "WG_AvailableModuleForThisSlot".Translate(),1000, hyperlinks: Links());
            }
        }
        private IEnumerable<Dialog_InfoCard.Hyperlink> Links()
        {
            foreach (ThingDef module in ModuleUtil.SlotUsers(this))
            {
                yield return new Dialog_InfoCard.Hyperlink(module);
            }
        }
    }
}
