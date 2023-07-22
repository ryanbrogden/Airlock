using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Dynamic;
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
        public class LightController
        {
            private IMyBlockGroup _blockGroup;
            private List<IMyLightingBlock> _lights = new List<IMyLightingBlock>();

            public LightController(IMyBlockGroup group)
            {
                _blockGroup = group;
                _blockGroup.GetBlocksOfType(_lights);
            }

            private void SetLightsRed()
            {
                _lights.ForEach(light =>
                {
                    light.Color = Color.Red;
                    light.Intensity = 2;
                    light.BlinkIntervalSeconds = 1.5f;
                    light.BlinkLength = 0.5f;
                });
            }

            private void SetLightsGreen()
            {
                _lights.ForEach(light =>
                {
                    light.Color = Color.Green;
                    light.Intensity = 2;
                    light.BlinkIntervalSeconds = 0;
                    light.BlinkLength = 0;
                });
            }

            public void Run(VentStatus ventStatus)
            {
                if (_lights.Count > 0)
                {
                    switch (ventStatus)
                    {
                        case VentStatus.Pressurizing:
                        case VentStatus.Depressurizing:
                            SetLightsRed();
                            break;
                        default:
                            SetLightsGreen();
                            break;
                    }
                }
            }
        }
    }
}
