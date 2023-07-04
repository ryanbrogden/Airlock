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
            private List<IMyTextPanel> textPanels = new List<IMyTextPanel>();
            private List<IMyAirVent> vents = new List<IMyAirVent>();
            private IMyParachute parachute;
            private int minimumAtmosphere = 90;

            private enum STATUS
            {
                ENABLED,
                DISABLED
            }

            private void Setup(MyGridProgram gridProgram, IMyBlockGroup group, int minimumAtmosphere)
            {
                this.program = gridProgram;
                this.minimumAtmosphere = minimumAtmosphere;
                sensorController = new SensorController(gridProgram, group);
                doorController = new DoorController(gridProgram, group);

                group.GetBlocksOfType(vents);
                group.GetBlocksOfType(textPanels);
            }

            public AirlockController(MyGridProgram gridProgram, IMyBlockGroup group, int minimumAtmosphere)
            {
                Setup(gridProgram, group, minimumAtmosphere);
            }

            public AirlockController(MyGridProgram gridProgram, IMyBlockGroup group, int minimumAtmosphere, IMyParachute parachute)
            {
                Setup(gridProgram, group, minimumAtmosphere);
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
                return vents.All(vent => vent.Status == VentStatus.Pressurized);
            }

            private bool isDepressurized()
            {
                return vents.All(vent => vent.Status == VentStatus.Depressurized || (vent.Status == VentStatus.Depressurizing && vent.GetOxygenLevel() < 0.5));
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
                        if (isDepressurized())
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
                }
            }

            private void ExternalControl()
            {
                switch (sensorController.LastState)
                {
                    case SENSOR_STATE.CLEAR:
                        if (isDepressurized())
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
                            doorController.Run(DOOR_STATE.CLOSED);
                            doorController.Disable();
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

                textPanels.ForEach(panel =>
                {
                    panel.ContentType = ContentType.TEXT_AND_IMAGE;
                    panel.Alignment = TextAlignment.CENTER;
                    panel.FontSize = 1.5f;
                    panel.WriteText($"Status: {Status()}");
                    panel.WriteText("\n", true);
                    panel.WriteText($"Atmosphere: {Atmosphere()}%", true);
                    panel.WriteText("\n", true);

                    if (Status() == STATUS.ENABLED)
                    {
                        panel.WriteText($"Current: {sensorController.CurrentState}", true);
                        panel.WriteText("\n", true);
                        panel.WriteText($"Last: {sensorController.LastState}", true);
                        panel.WriteText("\n", true);
                        panel.WriteText($"Doors: {doorController.Status}", true);
                        panel.WriteText("\n", true);
                        panel.WriteText($"Pressure: {vents[0].Status}", true);
                    }
                });
            }
        }
    }
}
