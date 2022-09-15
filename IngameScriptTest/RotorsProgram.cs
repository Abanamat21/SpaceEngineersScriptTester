using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    class RotorsProgram : MyGridProgram
    {
        public void Main(string argument, UpdateType updateSource)
        {
            IMyTextPanel panel = GridTerminalSystem.GetBlockWithName("Дисплей 1x1") as IMyTextPanel;
            StringBuilder sb = new StringBuilder();

            List<IMyMotorStator> rotors = new List<IMyMotorStator>();
            GridTerminalSystem.GetBlocksOfType(rotors);
            foreach (IMyMotorStator rotor in rotors)
            {
                //sb.AppendLine($"{rotor.DisplayNameText}");
                //sb.AppendLine($"Минимальный: {ShitToDeg(rotor.LowerLimitDeg)}");
                //sb.AppendLine($"Максимальный: {ShitToDeg(rotor.UpperLimitDeg)}");
                //sb.AppendLine($"Текущий: {ShitToDeg(rotor.Angle)}");
                rotor.LowerLimitRad = DegToRad(50);
                rotor.UpperLimitRad = DegToRad(100);
            }

            panel.WriteText(sb.ToString());
        }
        public float DegToRad(float input)
        {
            return (float)(Math.PI / 180 * input);
        }
        public string ShitToDeg(float input)
        {
            if (input == float.MaxValue || input == float.MinValue) return "Вечность";
            double angle = Math.Round(input * 180 / Math.PI);
            while (angle >= 360) angle = angle - 360;
            return angle.ToString();
        }
    }
}
