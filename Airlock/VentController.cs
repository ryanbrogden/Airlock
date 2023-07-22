using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    partial class Program
    {
        public class VentController
        {
            public int maxOxygen = 95;

            private List<IMyAirVent> vents = new List<IMyAirVent>();
            private List<IMyGasTank> oxygenTanks = new List<IMyGasTank>();
            private List<IMyTextPanel> panels = new List<IMyTextPanel>();

            public VentController(IMyGridTerminalSystem gridTerminalSystem, int maxOxygen)
            {
                gridTerminalSystem.GetBlocksOfType(panels, vent => vent.CustomName.ToUpper().Contains("[VENT]"));
                gridTerminalSystem.GetBlocksOfType(vents, vent => vent.CustomName.ToUpper().Contains("[VENT]"));
                gridTerminalSystem.GetBlocksOfType(oxygenTanks, tank => tank.DetailedInfo.Contains("Oxygen"));
                this.maxOxygen = maxOxygen;
            }

            public VentController(IMyGridTerminalSystem gridTerminalSystem)
            {
                gridTerminalSystem.GetBlocksOfType(panels, vent => vent.CustomName.ToUpper().Contains("[VENT]"));
                gridTerminalSystem.GetBlocksOfType(vents, vent => vent.CustomName.ToUpper().Contains("[VENT]"));
                gridTerminalSystem.GetBlocksOfType(oxygenTanks, tank => tank.DetailedInfo.Contains("Oxygen"));
            }

            public int GetTotalOxygen()
            {
                double capacity = oxygenTanks.Aggregate(0.0, (accumulator, currentTank) => accumulator += currentTank.Capacity);
                double currentStored = oxygenTanks.Aggregate(0.0, (accumulator, currentTank) =>
                {
                    double total = currentTank.Capacity * currentTank.FilledRatio;
                    return accumulator += total;
                });

                double systemFilled = (currentStored / capacity) * 100;

                return (int)Math.Ceiling(systemFilled);
            }

            public void Write()
            {
                panels.ForEach(panel =>
                {
                    panel.ContentType = ContentType.TEXT_AND_IMAGE;
                    panel.FontSize = 2;
                    panel.Alignment = TextAlignment.CENTER;
                    panel.WriteText($"{GetTotalOxygen()}%");
                });
            }
        }
    }
}
