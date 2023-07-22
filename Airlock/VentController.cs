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
using static IngameScript.Program;

namespace IngameScript
{
    partial class Program
    {
        public class VentController
        {
            public int _maxOxygen = 95;

            private List<IMyAirVent> vents = new List<IMyAirVent>();
            private List<IMyGasTank> oxygenTanks = new List<IMyGasTank>();
            private List<IMyTextPanel> panels = new List<IMyTextPanel>();

            private void Setup(IMyGridTerminalSystem gridTerminalSystem, IMyBlockGroup group)
            {
                gridTerminalSystem.GetBlocksOfType(panels, vent => vent.CustomName.ToUpper().Contains("[VENT]"));
                gridTerminalSystem.GetBlocksOfType(oxygenTanks, tank => tank.DetailedInfo.Contains("Oxygen"));
                group.GetBlocksOfType(vents);
            }

            public VentController(IMyGridTerminalSystem gridTerminalSystem, IMyBlockGroup group, int maxOxygen)
            {
                Setup(gridTerminalSystem, group);
                this._maxOxygen = maxOxygen;
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

            public bool isPressurized()
            {
                return vents.All(vent => vent.Status == VentStatus.Pressurized || (vent.Status == VentStatus.Pressurizing && vent.GetOxygenLevel() > 0.95));
            }

            public bool isDepressurized()
            {
                return vents.All(vent => vent.Status == VentStatus.Depressurized || (vent.Status == VentStatus.Depressurizing && vent.GetOxygenLevel() < 0.05));
            }

            public void Pressurize()
            {
                vents.ForEach(vent =>
                {
                    if (vent.CanPressurize)
                    {
                        vent.Depressurize = false;
                    }
                });
            }

            public void Depressurize()
            {
                vents.ForEach(vent => {
                    if (vent.CanPressurize)
                    {
                        vent.Depressurize = true;
                    }
                });
            }
        }
    }
}
