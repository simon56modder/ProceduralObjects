using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ColossalFramework.Plugins;
using UnityEngine;

namespace ProceduralObjects.Classes
{
    public static class TimeUtils
    {
        private static float inverseTimeScale, prevTimeScale = -19f, prevDeltaTime = -19f, multiplier;

        public static float deltaTime
        {
            get
            {
                if (prevTimeScale != Time.timeScale)
                {
                    inverseTimeScale = 1 / Time.timeScale;
                    prevTimeScale = Time.timeScale;
                    prevDeltaTime = Time.deltaTime;
                    multiplier = Time.deltaTime * inverseTimeScale;
                    return multiplier;
                }
                if (prevDeltaTime != Time.deltaTime)
                {
                    multiplier = Time.deltaTime * inverseTimeScale;
                    prevDeltaTime = Time.deltaTime;
                }
                return multiplier;
            }
        }
    }
}
