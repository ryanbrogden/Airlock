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
            private MyGridProgram _program;
            private SensorController _sensorController;
            private DoorController _doorController;
            private VentController _ventController;
            private LCDController _lcdController;
            private List<IMyTextPanel> textPanels = new List<IMyTextPanel>();
            private List<IMyAirVent> vents = new List<IMyAirVent>();
            private IMyParachute parachute;
            private int _minimumAtmosphere = 90;

            private enum STATUS
            {
                ENABLED,
                DISABLED
            }

            private void Setup(MyGridProgram gridProgram, IMyBlockGroup group, int minimumAtmosphere, int maxOxygen)
            {
                _lcdController = new LCDController(group, 10f, gridProgram.Runtime);
                _program = gridProgram;
                _minimumAtmosphere = minimumAtmosphere;
                _sensorController = new SensorController(gridProgram, group);
                _doorController = new DoorController(gridProgram, group);
                _ventController = new VentController(gridProgram.GridTerminalSystem, group, maxOxygen);

                group.GetBlocksOfType(vents);
                group.GetBlocksOfType(textPanels);
            }

            public AirlockController(MyGridProgram gridProgram, IMyBlockGroup group, int minimumAtmosphere, int maxOxygen)
            {
                Setup(gridProgram, group, minimumAtmosphere, maxOxygen);
            }

            public AirlockController(MyGridProgram gridProgram, IMyBlockGroup group, int minimumAtmosphere, int maxOxygen, IMyParachute parachute)
            {
                Setup(gridProgram, group, minimumAtmosphere, maxOxygen);
                this.parachute = parachute;
            }

            private int Atmosphere()
            {
                return (int)(parachute.Atmosphere * 100);
            }

            private STATUS Status()
            {
                return parachute != null && Atmosphere() < _minimumAtmosphere ? STATUS.ENABLED : STATUS.DISABLED;
            }

            private void Pressurize()
            {
                _doorController.Run(DOOR_STATE.CLOSED);
                if (_doorController.Status == DOOR_STATE.CLOSED)
                {
                    _doorController.Disable();
                    _ventController.Pressurize();
                }
            }

            private void Depressurize()
            {
                _doorController.Run(DOOR_STATE.CLOSED);
                if (_doorController.Status == DOOR_STATE.CLOSED)
                {
                    _doorController.Disable();
                    _ventController.Depressurize();
                }
            }

            private bool isPressurized()
            {
                return _ventController.isPressurized();
            }

            private bool isDepressurized()
            {
                return _ventController.isDepressurized();
            }

            private void InternalControl()
            {
                switch(_sensorController.LastState)
                {
                    case SENSOR_STATE.CLEAR:
                        if (isPressurized())
                        {
                            _doorController.Run(DOOR_STATE.INTERNAL_OPEN);
                        } else
                        {
                            Pressurize();
                        }
                        break;
                    case SENSOR_STATE.MIDDLE:
                        _doorController.Run(DOOR_STATE.CLOSED);
                        break;
                }
            }

            private void MiddleControl()
            {
                switch (_sensorController.LastState)
                {
                    case SENSOR_STATE.INTERNAL:
                        if (isDepressurized() || _ventController.GetTotalOxygen() > _ventController._maxOxygen)
                        {
                            _doorController.Run(DOOR_STATE.EXTERNAL_OPEN);
                        }
                        else
                        {
                            Depressurize();
                        }
                        break;
                    case SENSOR_STATE.EXTERNAL:
                        if (isPressurized())
                        {
                            _doorController.Run(DOOR_STATE.INTERNAL_OPEN);
                        }
                        else
                        {
                            Pressurize();
                        }
                        break;
                    case SENSOR_STATE.CLEAR:
                        _doorController.Run(DOOR_STATE.INTERNAL_OPEN);
                        break;
                }
            }

            private void ExternalControl()
            {
                switch (_sensorController.LastState)
                {
                    case SENSOR_STATE.CLEAR:
                        if (isDepressurized() || _ventController.GetTotalOxygen() > _ventController._maxOxygen)
                        {
                            _doorController.Run(DOOR_STATE.EXTERNAL_OPEN);
                        }
                        else
                        {
                            Depressurize();
                        }
                        break;
                    case SENSOR_STATE.MIDDLE:
                        _doorController.Run(DOOR_STATE.CLOSED);
                        break;
                }
            }

            private void ClearControl()
            {
                if (_ventController.GetTotalOxygen() > 98 && _doorController.Status == DOOR_STATE.CLOSED && isPressurized())
                {
                    _doorController.Run(DOOR_STATE.EXTERNAL_OPEN);
                }
                else
                {
                    _doorController.Run(DOOR_STATE.CLOSED);
                    Pressurize();
                    _doorController.Disable();
                }
            }

            private void Update()
            {

                if (Status() == STATUS.DISABLED)
                {
                    _ventController.Pressurize();
                    _doorController.Enable();
                    return;
                }

                if (_sensorController != null)
                {
                    switch (_sensorController.CurrentState)
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
                    _program.Echo("No SensorController");
                }
            }

            public void Run() {
                _sensorController.Run();
                Update();

                _lcdController.WriteText($"Status: {Status()}");
                _lcdController.WriteText("\n", true);
                _lcdController.WriteText($"Atmosphere: {Atmosphere()}%", true);
                _lcdController.WriteText("\n", true);

                if (Status() == STATUS.ENABLED)
                {
                    _lcdController.WriteText($"Current: {_sensorController.CurrentState}", true);
                    _lcdController.WriteText("\n", true);
                    _lcdController.WriteText($"Last: {_sensorController.LastState}", true);
                    _lcdController.WriteText("\n", true);
                    _lcdController.WriteText($"Doors: {_doorController.Status}", true);
                    _lcdController.WriteText("\n", true);
                    _lcdController.WriteText($"Pressure: {vents[0].Status}", true);
                    _lcdController.WriteText("\n", true);
                    _lcdController.WriteText($"System Oxygen: {_ventController.GetTotalOxygen()}%", true);
                    _lcdController.WriteText("\n", true);
                }
            }
        }
    }
}
