using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Exosuit
{
    public class GameComp_Tool:GameComponent
    {
        public override void FinalizeInit()
        {
            base.FinalizeInit();
            try
            {
                ClearCache();
            }
            catch { }
            try
            {
                postGameInit?.Invoke();
            }
            catch { }

        }
        private void ClearCache() {
            cacheCleaner?.Invoke();
        }

        public static void RegisterStaticCacheCleaner(Action a)
        {
            cacheCleaner += a;
        }
        public static void RegisterActionPostGameInit(Action a)
        {
            postGameInit += a;
        }
        private static Action cacheCleaner;
        private static Action postGameInit;
        public static GameComp_Tool instance;
        public GameComp_Tool(Game game)
        {
            instance = this;
        }

        
    }
}
