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
            private List<IMyDoor> internalDoor, externalDoor = new List<IMyDoor>();

            private DoorStatus IDoorState
            {
                get
                {
                    bool isAllClosed = internalDoor.All(door => door.Status == DoorStatus.Closed);
                    return isAllClosed ? DoorStatus.Closed : DoorStatus.Open;
                }
            }

            private DoorStatus EDoorState
            {
                get
                {
                    bool isAllClosed = externalDoor.All(door => door.Status == DoorStatus.Closed);
                    return isAllClosed ? DoorStatus.Closed : DoorStatus.Open;
                }
            }

            public DOOR_STATE Status
            { 
                get
                {
                    if (IDoorState == DoorStatus.Closed && EDoorState == DoorStatus.Closed)
                    {
                        return DOOR_STATE.CLOSED;
                    } else if (EDoorState == DoorStatus.Open)
                    {
                        return DOOR_STATE.EXTERNAL_OPEN;
                    }
                    else if (IDoorState == DoorStatus.Open)
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

                internalDoor = doors.FindAll(door => door.CustomName.ToUpper().Contains(SENSOR_STATE.INTERNAL.ToString()));
                externalDoor = doors.FindAll(door => door.CustomName.ToUpper().Contains(SENSOR_STATE.EXTERNAL.ToString()));

                if (internalDoor.Count == 0)
                {
                    program.Echo("Internal door could not be found");
                }
                if (externalDoor.Count == 0)
                {
                    program.Echo("External door could not be found");
                }
            }

            private void OpenInternalDoor()
            {
                
                CloseDoors();
                Disable();

                internalDoor.ForEach(door => { 
                    door.Enabled = true;
                    door.OpenDoor();
                });
            }

            private void OpenExternalDoor()
            {
                CloseDoors();
                Disable();

                externalDoor.ForEach(door => {
                    door.Enabled = true;
                    door.OpenDoor();
                });
            }


            private void CloseDoors()
            {
                if (Status != DOOR_STATE.CLOSED)
                {
                    internalDoor.ForEach(door =>
                    {
                        door.CloseDoor();
                    });

                    externalDoor.ForEach(door =>
                    {
                        door.CloseDoor();
                    });
                }
            }

            public void Disable()
            {
                if (Status == DOOR_STATE.CLOSED)
                {
                    internalDoor.ForEach((door) =>
                    {
                        door.Enabled = false;
                    });

                    externalDoor.ForEach((door) =>
                    {
                        door.Enabled = false;
                    });
                }
            }

            public void Enable()
            {
                internalDoor.ForEach((door) =>
                {
                    door.Enabled = true;
                });

                externalDoor.ForEach((door) =>
                {
                    door.Enabled = true;
                });
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
