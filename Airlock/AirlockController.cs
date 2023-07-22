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
        public class AirlockController
        {
            private MyGridProgram program;
            private SensorController sensorController;
            private DoorController doorController;
            private VentController ventController;
            private LCDController LCDController;
            private List<IMyTextPanel> textPanels = new List<IMyTextPanel>();
            private List<IMyAirVent> vents = new List<IMyAirVent>();
            private IMyParachute parachute;
            private int minimumAtmosphere = 90;

            private enum STATUS
            {
                ENABLED,
                DISABLED
            }

            private void Setup(MyGridProgram gridProgram, IMyBlockGroup group, int minimumAtmosphere, VentController ventController)
            {
                LCDController = new LCDController(group, 10f, gridProgram.Runtime);
                program = gridProgram;
                this.minimumAtmosphere = minimumAtmosphere;
                sensorController = new SensorController(gridProgram, group);
                doorController = new DoorController(gridProgram, group);
                this.ventController = ventController;

                group.GetBlocksOfType(vents);
                group.GetBlocksOfType(textPanels);
            }

            public AirlockController(MyGridProgram gridProgram, IMyBlockGroup group, int minimumAtmosphere, VentController ventController)
            {
                Setup(gridProgram, group, minimumAtmosphere, ventController);
            }

            public AirlockController(MyGridProgram gridProgram, IMyBlockGroup group, int minimumAtmosphere, VentController ventController, IMyParachute parachute)
            {
                Setup(gridProgram, group, minimumAtmosphere, ventController);
                this.parachute = parachute;
            }

            private int Atmosphere()
            {
                return (int)(parachute.Atmosphere * 100);
            }

            private STATUS Status()
            {
                return parachute != null && Atmosphere() < minimumAtmosphere ? STATUS.ENABLED : STATUS.DISABLED;
            }

            private void Pressurize()
            {
                doorController.Run(DOOR_STATE.CLOSED);
                if (doorController.Status == DOOR_STATE.CLOSED)
                {
                    doorController.Disable();
                    vents.ForEach(vent =>
                    {
                        if (vent.CanPressurize)
                        {
                            vent.Depressurize = false;
                        }
                    });
                }
            }

            private void Depressurize()
            {
                doorController.Run(DOOR_STATE.CLOSED);
                if (doorController.Status == DOOR_STATE.CLOSED)
                {
                    doorController.Disable();
                    vents.ForEach(vent => {
                        if (vent.CanPressurize)
                        {
                            vent.Depressurize = true;
                        }
                    });
                }
            }

            private bool isPressurized()
            {
                return vents.All(vent => vent.Status == VentStatus.Pressurized || (vent.Status == VentStatus.Pressurizing && vent.GetOxygenLevel() > 0.95));
            }

            private bool isDepressurized()
            {
                return vents.All(vent => vent.Status == VentStatus.Depressurized || (vent.Status == VentStatus.Depressurizing && vent.GetOxygenLevel() < 0.05));
            }

            private void InternalControl()
            {
                switch(sensorController.LastState)
                {
                    case SENSOR_STATE.CLEAR:
                        if (isPressurized())
                        {
                            doorController.Run(DOOR_STATE.INTERNAL_OPEN);
                        } else
                        {
                            Pressurize();
                        }
                        break;
                    case SENSOR_STATE.MIDDLE:
                        doorController.Run(DOOR_STATE.CLOSED);
                        break;
                }
            }

            private void MiddleControl()
            {
                switch (sensorController.LastState)
                {
                    case SENSOR_STATE.INTERNAL:
                        if (isDepressurized() || ventController.GetTotalOxygen() > ventController.maxOxygen)
                        {
                            doorController.Run(DOOR_STATE.EXTERNAL_OPEN);
                        }
                        else
                        {
                            Depressurize();
                        }
                        break;
                    case SENSOR_STATE.EXTERNAL:
                        if (isPressurized())
                        {
                            doorController.Run(DOOR_STATE.INTERNAL_OPEN);
                        }
                        else
                        {
                            Pressurize();
                        }
                        break;
                    case SENSOR_STATE.CLEAR:
                        doorController.Run(DOOR_STATE.INTERNAL_OPEN);
                        break;
                }
            }

            private void ExternalControl()
            {
                switch (sensorController.LastState)
                {
                    case SENSOR_STATE.CLEAR:
                        if (isDepressurized() || ventController.GetTotalOxygen() > ventController.maxOxygen)
                        {
                            doorController.Run(DOOR_STATE.EXTERNAL_OPEN);
                        }
                        else
                        {
                            Depressurize();
                        }
                        break;
                    case SENSOR_STATE.MIDDLE:
                        doorController.Run(DOOR_STATE.CLOSED);
                        break;
                }
            }

            private void ClearControl()
            {
                if (ventController.GetTotalOxygen() > 98 && doorController.Status == DOOR_STATE.CLOSED && isPressurized())
                {
                    doorController.Run(DOOR_STATE.EXTERNAL_OPEN);
                }
                else
                {
                    doorController.Run(DOOR_STATE.CLOSED);
                    Pressurize();
                    doorController.Disable();
                }
            }

            private void Update()
            {

                if (Status() == STATUS.DISABLED)
                {
                    vents.ForEach(vent =>
                    {
                        if (vent.CanPressurize)
                        {
                            vent.Depressurize = false;
                        }
                    });
                    doorController.Enable();
                    return;
                }

                if (sensorController != null)
                {
                    switch (sensorController.CurrentState)
                    {
                        case SENSOR_STATE.INTERNAL:
                            InternalControl();
                            break;
                        case SENSOR_STATE.MIDDLE:
                            MiddleControl();
                            break;
                        case SENSOR_STATE.EXTERNAL:
                            ExternalControl();
                            break;
                        default:
                            ClearControl();
                            break;

                    }
                } else
                {
                    program.Echo("No SensorController");
                }
            }

            public void Run() {
                sensorController.Run();
                Update();

                LCDController.WriteText($"Status: {Status()}");
                LCDController.WriteText("\n", true);
                LCDController.WriteText($"Atmosphere: {Atmosphere()}%", true);
                LCDController.WriteText("\n", true);

                if (Status() == STATUS.ENABLED)
                {
                    LCDController.WriteText($"Current: {sensorController.CurrentState}", true);
                    LCDController.WriteText("\n", true);
                    LCDController.WriteText($"Last: {sensorController.LastState}", true);
                    LCDController.WriteText("\n", true);
                    LCDController.WriteText($"Doors: {doorController.Status}", true);
                    LCDController.WriteText("\n", true);
                    LCDController.WriteText($"Pressure: {vents[0].Status}", true);
                    LCDController.WriteText("\n", true);
                    LCDController.WriteText($"System Oxygen: {ventController.GetTotalOxygen()}%", true);
                    LCDController.WriteText("\n", true);
                }
            }
        }
    }
}
