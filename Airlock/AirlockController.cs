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
            private LightController _lightController;
            private List<IMyTextPanel> textPanels = new List<IMyTextPanel>();
            private List<IMyAirVent> vents = new List<IMyAirVent>();
            private IMyParachute _parachute;
            private int _minimumAtmosphere = 90;
            private string _groupName = "";
            private IMyBlockGroup _blockGroup;

            private enum STATUS
            {
                ENABLED,
                DISABLED
            }

            private void Setup(MyGridProgram gridProgram, string groupName, int minimumAtmosphere, int maxOxygen)
            {
                _groupName = groupName;
                _blockGroup = gridProgram.GridTerminalSystem.GetBlockGroupWithName(_groupName);

                if (_blockGroup != null)
                {
                    _lcdController = new LCDController(_blockGroup, 10f, gridProgram.Runtime);
                    _program = gridProgram;
                    _minimumAtmosphere = minimumAtmosphere;
                    _sensorController = new SensorController(gridProgram, _blockGroup);
                    _doorController = new DoorController(gridProgram, _blockGroup);
                    _ventController = new VentController(gridProgram.GridTerminalSystem, _blockGroup, maxOxygen);
                    _lightController = new LightController(_blockGroup);

                    _blockGroup.GetBlocksOfType(vents);
                    _blockGroup.GetBlocksOfType(textPanels);
                }
                else
                {
                    gridProgram.Echo($"Group name \"{groupName}\" not found");
                }
            }

            public AirlockController(MyGridProgram gridProgram, string groupName, int minimumAtmosphere, int maxOxygen)
            {
                Setup(gridProgram, groupName, minimumAtmosphere, maxOxygen);
            }

            public AirlockController(MyGridProgram gridProgram, string groupName, int minimumAtmosphere, int maxOxygen, IMyParachute parachute)
            {
                Setup(gridProgram, groupName, minimumAtmosphere, maxOxygen);
                _parachute = parachute;
            }

            private int Atmosphere
            {
                get 
                {
                    return (int)(_parachute.Atmosphere * 100);
                }
            }

            private STATUS Status
            {
                get
                {
                    return _parachute != null && Atmosphere < _minimumAtmosphere ? STATUS.ENABLED : STATUS.DISABLED;
                }
            }

            private void PressurizeAirlock()
            {
                _doorController.Run(DOOR_STATE.CLOSED);
                if (_doorController.Status == DOOR_STATE.CLOSED)
                {
                    _doorController.Disable();
                    _ventController.Pressurize();
                }
            }

            private void DepressurizeAirlock()
            {
                _doorController.Run(DOOR_STATE.CLOSED);
                if (_doorController.Status == DOOR_STATE.CLOSED)
                {
                    _doorController.Disable();
                    _ventController.Depressurize();
                }
            }

            private void InternalControl()
            {
                switch(_sensorController.LastState)
                {
                    case SENSOR_STATE.CLEAR:
                        if (_ventController.isPressurized())
                        {
                            _doorController.Run(DOOR_STATE.INTERNAL_OPEN);
                        } else
                        {
                            PressurizeAirlock();
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
                        if (_ventController.isDepressurized() || _ventController.GetTotalOxygen() > _ventController._maxOxygen)
                        {
                            _doorController.Run(DOOR_STATE.EXTERNAL_OPEN);
                        }
                        else
                        {
                            DepressurizeAirlock();
                        }
                        break;
                    case SENSOR_STATE.EXTERNAL:
                        if (_ventController.isPressurized())
                        {
                            _doorController.Run(DOOR_STATE.INTERNAL_OPEN);
                        }
                        else
                        {
                            PressurizeAirlock();
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
                        if (_ventController.isDepressurized() || _ventController.GetTotalOxygen() > _ventController._maxOxygen)
                        {
                            _doorController.Run(DOOR_STATE.EXTERNAL_OPEN);
                        }
                        else
                        {
                            DepressurizeAirlock();
                        }
                        break;
                    case SENSOR_STATE.MIDDLE:
                        _doorController.Run(DOOR_STATE.CLOSED);
                        break;
                }
            }

            private void ClearControl()
            {
                if (_ventController.GetTotalOxygen() > 98 && _doorController.Status == DOOR_STATE.CLOSED && _ventController.isPressurized())
                {
                    _doorController.Run(DOOR_STATE.EXTERNAL_OPEN);
                }
                else
                {
                    _doorController.Run(DOOR_STATE.CLOSED);
                    PressurizeAirlock();
                    _doorController.Disable();
                }
            }

            private void Update()
            {

                if (Status == STATUS.DISABLED)
                {
                    PressurizeAirlock();
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

                if (_blockGroup != null)
                {
                    _sensorController.Run();
                    _lightController.Run(_ventController.Status);
                    Update();

                    _lcdController.WriteText($"{_groupName}");
                    _lcdController.WriteText("\n", true);
                    _lcdController.WriteText($"Status: {Status}", true);
                    _lcdController.WriteText("\n", true);
                    _lcdController.WriteText($"Atmosphere: {Atmosphere}%", true);
                    _lcdController.WriteText("\n", true);

                    if (Status == STATUS.ENABLED)
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
}
