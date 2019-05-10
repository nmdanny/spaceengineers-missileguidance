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
        public class Logger
        {
            List<IMyTextSurface> surfaces = new List<IMyTextSurface>();
            public Logger(Program prog,string requiredIniSection, bool enforceSameCubegrid=true)
            {
                var blocks = new List<IMyTerminalBlock>();
                prog.GridTerminalSystem.GetBlocksOfType(blocks, bl =>
                {
                    var hasSection = MyIni.HasSection(bl.CustomData, requiredIniSection);
                    var sameCubegrid = enforceSameCubegrid ? bl.CubeGrid == prog.Me.CubeGrid : true;
                    return hasSection && sameCubegrid;
                });
                foreach (var block in blocks) { 
                    if (block is IMyTextSurface)
                    {
                        prog.Echo($"Found text surface \"{block.CustomName}\" for logging.");
                        AddSurface((IMyTextSurface)block);
                    }
                    else if (block is IMyTextSurfaceProvider)
                    {
                        prog.Echo($"Found text surface provider \"{block.CustomName}\" for logging.");
                        AddSurface(((IMyTextSurfaceProvider)block).GetSurface(0));
                    }
                }
                if (surfaces.Count == 0)
                {
                    prog.Echo("Warning: Couldn't find any displays during logger initialization!");
                }
            }

            private void AddSurface(IMyTextSurface sfc)
            {
                sfc.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                sfc.WriteText(string.Empty, false);
                this.surfaces.Add(sfc);
            }
        public void Output(string msg, bool append = false)
            {
                foreach (var surf in surfaces)
                {
                    surf.WriteText(msg, append);
                }
            }
            public void OutputLine(string msg, bool append = false)
            {
                Output(msg + Environment.NewLine, append);
            }

            public void ResetDisplay()
            {
                foreach (var surf in surfaces)
                {
                    surf.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    surf.WriteText(string.Empty, false);
                }
            }
        }

        private void ResetDisplay()
        {
            Me.GetSurface(0).ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
            Me.GetSurface(0).WriteText(string.Empty, false);
        }

        public static string GetRandomString(int length)
        {
            Random rand = new Random();
            const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(alphabet, length)
                .Select(s => s[rand.Next(s.Length)]).ToArray());
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
