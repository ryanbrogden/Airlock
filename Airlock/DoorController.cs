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
    public enum DOOR_STATE
    {
        CLOSED,
        INTERNAL_OPEN,
        EXTERNAL_OPEN,
        IN_PROGRESS
    }

    partial class Program
    {
        public class DoorController
        {
            private MyGridProgram program;
            private IMyDoor internalDoor, externalDoor;

            public DOOR_STATE Status
            { 
                get
                {
                    if (internalDoor.Status == DoorStatus.Closed && externalDoor.Status == DoorStatus.Closed)
                    {
                        return DOOR_STATE.CLOSED;
                    } else if (externalDoor.Status == DoorStatus.Open)
                    {
                        return DOOR_STATE.EXTERNAL_OPEN;
                    }
                    else if (internalDoor.Status == DoorStatus.Open)
                    {
                        return DOOR_STATE.INTERNAL_OPEN;
                    }

                    return DOOR_STATE.IN_PROGRESS;
                }
            }

            public DoorController(MyGridProgram gridProgram, IMyBlockGroup group)
            {
                this.program = gridProgram;
                List<IMyDoor> doors = new List<IMyDoor>();
                group.GetBlocksOfType(doors);

                doors.ForEach(door => door.CloseDoor());

                internalDoor = doors.Find(door => door.CustomName.ToUpper().Contains(SENSOR_STATE.INTERNAL.ToString()));
                externalDoor = doors.Find(door => door.CustomName.ToUpper().Contains(SENSOR_STATE.EXTERNAL.ToString()));

                if (internalDoor == null)
                {
                    program.Echo("Internal door could not be found");
                }
                if (externalDoor == null)
                {
                    program.Echo("External door could not be found");
                }
            }

            private void OpenInternalDoor()
            {
                internalDoor.Enabled = true;
                internalDoor.OpenDoor();
            }

            private void OpenExternalDoor()
            {
                externalDoor.Enabled = true;
                externalDoor.OpenDoor();
            }

            private void CloseDoors()
            {
                internalDoor.CloseDoor();
                externalDoor.CloseDoor();
            }

            public void Disable()
            {
                if (Status == DOOR_STATE.CLOSED)
                {
                    internalDoor.Enabled = false;
                    externalDoor.Enabled = false;
                }
            }

            public void Run(DOOR_STATE desiredState)
            {
                switch(desiredState) 
                {
                    case DOOR_STATE.CLOSED:
                        CloseDoors();
                        break;
                    case DOOR_STATE.INTERNAL_OPEN: 
                        OpenInternalDoor();
                        break;
                    case DOOR_STATE.EXTERNAL_OPEN:
                        OpenExternalDoor();
                        break;
                }
            }

            
        }
    }
}
