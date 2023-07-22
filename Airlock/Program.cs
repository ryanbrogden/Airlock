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
    partial class Program : MyGridProgram
    {
        // This file contains your actual script.
        //
        // You can either keep all your code here, or you can create separate
        // code files to make your program easier to navigate while coding.
        //
        // In order to add a new utility class, right-click on your project, 
        // select 'New' then 'Add Item...'. Now find the 'Space Engineers'
        // category under 'Visual C# Items' on the left hand side, and select
        // 'Utility Class' in the main area. Name it in the box below, and
        // press OK. This utility class will be merged in with your code when
        // deploying your final script.
        //
        // You can also simply create a new utility class manually, you don't
        // have to use the template if you don't want to. Just do so the first
        // time to see what a utility class looks like.
        // 
        // Go to:
        // https://github.com/malware-dev/MDK-SE/wiki/Quick-Introduction-to-Space-Engineers-Ingame-Scripts
        //
        // to learn more about ingame scripts.
        private MyIni _ini = new MyIni();

        private List<AirlockController> airlockControllers = new List<AirlockController>();
        private VentController ventController;

        public Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.
            // 
            // It's recommended to set Runtime.UpdateFrequency 
            // here, which will allow your script to run itself without a 
            // timer block.
            Runtime.UpdateFrequency = UpdateFrequency.Update100;


            List<IMyParachute> parachutes = new List<IMyParachute>();
            GridTerminalSystem.GetBlocksOfType(parachutes);

            MyIniParseResult parseResult;
            if (!_ini.TryParse(Me.CustomData, out parseResult))
            {
                throw new Exception(parseResult.ToString());
            }
            else
            {
                string groupNameData = _ini.Get("Airlock", "groups").ToString();
                string[] groupNames = groupNameData.Split(',');
                int minimumAtmosphere = int.Parse(_ini.Get("Airlock", "minimumAtmosphere").ToString());

                int maxOxygen;
                
                try
                {
                    maxOxygen = int.Parse(_ini.Get("Airlock", "maxOxygen").ToString());
                } catch
                {
                    maxOxygen = 0;
                }

                if (maxOxygen > 0)
                {
                    ventController = new VentController(GridTerminalSystem, maxOxygen);
                }
                else
                {
                    ventController = new VentController(GridTerminalSystem);
                }
                

                for (int i = 0; i < groupNames.Length; i++)
                {
                    IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName(groupNames[i]);

                    if (group != null)
                    {
                        Echo($"Group name \"{groupNames[i]}\" located");
                        if (parachutes.Count > 0)
                        {
                            airlockControllers.Add(new AirlockController(this, group, minimumAtmosphere, ventController, parachutes[0] ?? null));
                        } else
                        {
                            airlockControllers.Add(new AirlockController(this, group, minimumAtmosphere, ventController));
                        }
                    }
                    else
                    {
                        Echo($"Group name \"{groupNames[i]}\" not found");
                    }

                }
            }
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (airlockControllers.Count > 0)
            {
                airlockControllers.ForEach(controller => controller.Run());
            }
            ventController.Write();
        }
    }
}
