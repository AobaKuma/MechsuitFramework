using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Exosuit
{
    public class CommandActionWithOptions:Command_Action
    {
        public List<FloatMenuOption> options = [];
        public override IEnumerable<FloatMenuOption> RightClickFloatMenuOptions => options;
        public override void MergeWith(Gizmo other)
        {
            options.AddRange(((CommandActionWithOptions)other).options);
            
        }
    }
}
