using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        private void ResetDisplay()
        {
            Me.GetSurface(0).ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
            Me.GetSurface(0).WriteText(string.Empty, false);
        }
    }
    public static class Extensions
    {
        public static T GetBlockOfType<T>(this IMyGridTerminalSystem gts, Func<T, bool> collect = null) where T : class
        {
            var lst = new List<T>();
            gts.GetBlocksOfType<T>(lst, collect);
            if (lst.Count == 0)
            {
                return null;
            }
            return lst[0];
        }
    }
}
