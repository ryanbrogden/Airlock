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
    public enum SENSOR_STATE
    {
        INTERNAL,
        EXTERNAL,
        MIDDLE,
        CLEAR
    }

    partial class Program
    {
        public class SensorController
        {
            private MyGridProgram program;
            private List<IMySensorBlock> sensors = new List<IMySensorBlock>();
            private SENSOR_STATE currentState = SENSOR_STATE.CLEAR;
            private SENSOR_STATE lastState = SENSOR_STATE.CLEAR;

            public SENSOR_STATE CurrentState
            {
                get
                {
                    return currentState;
                }
            }

            public SENSOR_STATE LastState
            {
                get
                {
                    return lastState;
                }
            }

            public SensorController(MyGridProgram gridProgram, IMyBlockGroup group)
            {
                this.program = gridProgram;
                group.GetBlocksOfType(sensors);

                sensors.ForEach(sensor => {
                    sensor.DetectFriendly = true;
                    sensor.DetectLargeShips = false;
                    sensor.DetectSmallShips = false;
                    sensor.DetectEnemy = false;
                    sensor.DetectNeutral = false;
                    sensor.Enabled = true;
                });
            }

            private IMySensorBlock InternalSensor
            {
                get
                {
                    return sensors.Find(sensor => sensor.CustomName.ToUpper().Contains(SENSOR_STATE.INTERNAL.ToString()));
                }
            }

            private IMySensorBlock ExternalSensor
            {
                get
                {
                    return sensors.Find(sensor => sensor.CustomName.ToUpper().Contains(SENSOR_STATE.EXTERNAL.ToString()));
                }
            }

            public IMySensorBlock GetActiveSensor()
            {
                return sensors.Find((sensor) => sensor.IsActive);
            }

            private void UpdateState()
            {
                IMySensorBlock activeSensor = GetActiveSensor();
                SENSOR_STATE newState = SENSOR_STATE.CLEAR;

                if (activeSensor != null)
                {
                    List<SENSOR_STATE> states = new List<SENSOR_STATE>() { SENSOR_STATE.INTERNAL, SENSOR_STATE.EXTERNAL, SENSOR_STATE.MIDDLE };
                    newState = states.First(state => activeSensor.CustomName.ToUpper().Contains(state.ToString()));
                }


                if (newState != CurrentState)
                {
                    lastState = CurrentState;
                    currentState = newState;
                }

                switch (CurrentState)
                {
                    case SENSOR_STATE.INTERNAL:
                        ExternalSensor.Enabled = false;
                        break;
                    case SENSOR_STATE.MIDDLE:
                        InternalSensor.Enabled = false;
                        ExternalSensor.Enabled = false;
                        break;
                    case SENSOR_STATE.EXTERNAL:
                        InternalSensor.Enabled = false;
                        break;
                    default:
                        InternalSensor.Enabled = true;
                        ExternalSensor.Enabled = true;
                        break;

                }
            }

            public void Run()
            {
                if (sensors.Count > 0)
                {
                    UpdateState();
                }
                else
                {
                    program.Echo("No sensors found");
                }
            }
        }
    }
}
