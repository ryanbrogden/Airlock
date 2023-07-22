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
    partial class Program
    {
        public class LCDController
        {
            private List<IMyTextPanel> lcdPanels;
            private float scrollOffset;
            private const float BaseFontSize = 1.2f;
            private const float MaxFontSize = 3.0f;
            private const float MinFontSize = 0.5f;
            private float scrollSpeed;
            private IMyGridProgramRuntimeInfo runtime;

            public LCDController(IMyBlockGroup lcdGroup, float speed, IMyGridProgramRuntimeInfo programRuntime)
            {
                lcdPanels = new List<IMyTextPanel>();
                scrollOffset = 0f;
                scrollSpeed = speed;
                runtime = programRuntime;
                InitializeLCDPanels(lcdGroup);
            }

            private void InitializeLCDPanels(IMyBlockGroup lcdGroup)
            {
                lcdPanels.Clear();
                lcdGroup.GetBlocksOfType(lcdPanels);
                foreach (IMyTextPanel lcdPanel in lcdPanels)
                {
                    lcdPanel.ContentType = ContentType.TEXT_AND_IMAGE;
                }
            }

            public void WriteText(string text, bool append = false)
            {
                float fontSize = CalculateFontSize(text);

                foreach (IMyTextPanel lcdPanel in lcdPanels)
                {
                    lcdPanel.FontSize = fontSize;

                    if (fontSize < MinFontSize)
                    {
                        scrollOffset += scrollSpeed * (float)runtime.TimeSinceLastRun.TotalSeconds;
                        scrollOffset %= lcdPanel.SurfaceSize.Y;
                        text = ScrollText(text, scrollOffset);
                    }

                    lcdPanel.WriteText(text, append);
                }
            }

            private float CalculateFontSize(string text)
            {
                IMyTextPanel lcdPanel = lcdPanels.Count > 0 ? lcdPanels[0] : null;

                if (lcdPanel == null)
                {
                    return BaseFontSize;
                }

                float scaleFactorHorizontal = lcdPanel.SurfaceSize.X / lcdPanel.GetValue<Single>("FontSize");
                float scaleFactorVertical = lcdPanel.SurfaceSize.Y / lcdPanel.GetValue<Single>("FontSize");

                float fontSizeHorizontal = BaseFontSize * Math.Min(scaleFactorHorizontal, 1.0f);
                float fontSizeVertical = BaseFontSize * Math.Min(scaleFactorVertical, 1.0f);

                if (fontSizeHorizontal > MaxFontSize || fontSizeVertical > MaxFontSize)
                {
                    return MaxFontSize;
                }

                if (fontSizeVertical < MinFontSize)
                {
                    return fontSizeVertical;
                }

                return fontSizeVertical;
            }

            private string ScrollText(string text, float offset)
            {
                IMyTextPanel lcdPanel = lcdPanels.Count > 0 ? lcdPanels[0] : null;

                if (lcdPanel == null)
                {
                    return text;
                }

                int startIndex = (int)(text.Length * offset / lcdPanel.SurfaceSize.Y);
                startIndex %= text.Length;

                return text.Substring(startIndex) + text.Substring(0, startIndex);
            }
        }
    }
}
