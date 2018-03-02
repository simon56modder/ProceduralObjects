using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProceduralObjects.Tools
{
    public class ProceduralTool : ToolBase
    {
        // basically Empty ATM, all the related code is located in //ProceduralObjectsLogic.cs
    }
    public static class ToolHelper
    {
        public static void FullySetTool<T>() where T : ToolBase
        {
            ToolsModifierControl.toolController.CurrentTool = ToolsModifierControl.GetTool<T>();
            ToolsModifierControl.SetTool<T>();
           // ToolsModifierControl.mainToolbar.CloseEverything();
        }
    }
}
