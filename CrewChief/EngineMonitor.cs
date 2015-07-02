using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChief.Data;

// Oil temps are typically 1 or 2 units (I'm assuming celcius) higher than water temps. Typical temps while racing tend to be
// mid - high 50s, with some in-traffic running this creeps up to the mid 60s. To get it into the 
// 70s you have to really try. Any higher requires you to sit by the side of the road bouncing off the
// rev limiter. Doing this I've been able to get to 110 without blowing up (I got bored). With temps in the
// 80s, by the end of a single lap at racing speed they're back into the 60s.
//
// I think the cool down effect of the radiator is the underlying issue here - it's far too strong. 
// The oil temp does lag behind the water temp, which is correct, but I think it should lag 
// more (i.e. it should take longer for the oil to cool) and the oil should heat up more relative to the water. 
// 
// I'd expect to be seeing water temperatures in the 80s for 'normal' running, with this getting well into the 
// 90s or 100s in traffic. The oil temps should be 100+, maybe hitting 125 or more when the water's also hot.
// 
// The warning thresholds in the config file have to take this apparent inaccuracy into account, so I've
// assumed the water temps are really about 30 degrees hotter than the reported temps. So the water 'hot' warning
// threshold is at 65. As the oil temps seem too close to water temps in the sim, I've assumed the real oil
// temps are about 50 degrees hotter than the reported temps, so the oil 'hot' threshold is at 70.
// 
// These values aren't realistic and the warnings generated should be taken with a pinch of salt. They 
// sound cool, but you're not likely to blow your engine if the temps are in the 70s. If I used realistic
// values for these thresholds you'd *never* hear the warnings. Ever. When S3 addresses the underlying issue
// these thresholds can be set to something more realistic

namespace CrewChief.Events
{
    class EngineMonitor : AbstractEvent
    {
        private String folderAllClear = "engine_monitor/all_clear";
        private String folderHotWater = "engine_monitor/hot_water";
        private String folderHotOil = "engine_monitor/hot_oil";
        private String folderHotOilAndWater = "engine_monitor/hot_oil_and_water";
        private String folderLowOilPressure = "engine_monitor/low_oil_pressure";

        private static float maxSafeWaterTemp = Properties.Settings.Default.max_safe_water_temp;
        private static float maxSafeOilTemp = Properties.Settings.Default.max_safe_oil_temp;
        private static Boolean logTemps = Properties.Settings.Default.log_temps;

        double lastDataPointGameTime;

        EngineStatus lastStatusMessage;

        EngineData engineData;

        // record engine data for 30 seconds then report changes
        double statusMonitorWindowLength = 30;

        double gameTimeAtLastStatusCheck;

        public EngineMonitor(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        protected override void clearStateInternal()
        {
            lastDataPointGameTime = 0;
            lastStatusMessage = EngineStatus.ALL_CLEAR;
            engineData = new EngineData();
            gameTimeAtLastStatusCheck = 0;
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return true;
        }

        override protected void triggerInternal(Shared lastState, Shared currentState)
        {
            if (currentState.Player.GameSimulationTime > lastDataPointGameTime)
            {
                if (engineData == null)
                {
                    clearStateInternal();
                }
                if (currentState.Player.GameSimulationTime > gameTimeAtLastStatusCheck + statusMonitorWindowLength)
                {
                    // we have 30 seconds of engine data, so check it and reset it
                    EngineStatus currentEngineStatus = engineData.getEngineStatus();
                    if (currentEngineStatus != lastStatusMessage)
                    {
                        switch (currentEngineStatus)
                        {
                            case EngineStatus.ALL_CLEAR:
                                audioPlayer.queueClip(folderAllClear, 0, this);
                                lastStatusMessage = currentEngineStatus;
                                break;
                            case EngineStatus.HOT_OIL:
                                // don't play this if the last message was about hot oil *and* water - wait for 'all clear'
                                if (lastStatusMessage != EngineStatus.HOT_OIL_AND_WATER)
                                {
                                    audioPlayer.queueClip(folderHotOil, 0, this);
                                    lastStatusMessage = currentEngineStatus;
                                }                                
                                break;
                            case EngineStatus.HOT_WATER:
                                // don't play this if the last message was about hot oil *and* water - wait for 'all clear'
                                if (lastStatusMessage != EngineStatus.HOT_OIL_AND_WATER)
                                {
                                    audioPlayer.queueClip(folderHotWater, 0, this);
                                    lastStatusMessage = currentEngineStatus;
                                }
                                break;
                            case EngineStatus.HOT_OIL_AND_WATER:
                                audioPlayer.queueClip(folderHotOilAndWater, 0, this);
                                lastStatusMessage = currentEngineStatus;
                                break;
                            case EngineStatus.LOW_OIL_PRESSURE:
                                audioPlayer.queueClip(folderLowOilPressure, 0, this);
                                lastStatusMessage = currentEngineStatus;
                                break;
                        }
                    }
                    gameTimeAtLastStatusCheck = currentState.Player.GameSimulationTime;
                    engineData = new EngineData();
                }
                if (engineData == null)
                {
                    clearStateInternal();
                }
                engineData.addSample(currentState);
                if (logTemps)
                {
                    Console.WriteLine(currentState.EngineWaterTemp + ", " + currentState.EngineOilTemp + ", " + currentState.EngineOilPressure);
                }
                lastDataPointGameTime = currentState.Player.GameSimulationTime;
            }
        }

        private class EngineData
        {
            private int samples;
            private float oilTemp;
            private float waterTemp;
            private float oilPressure;
            public EngineData () 
            {
                this.samples = 0;
                this.oilPressure = 0;
                this.oilTemp = 0;
                this.waterTemp = 0;
            }
            public void addSample(Shared currentData)
            {
                this.samples++;
                this.oilTemp += currentData.EngineOilTemp;
                this.waterTemp += currentData.EngineWaterTemp;
                this.oilPressure += currentData.EngineOilPressure;
            }
            public EngineStatus getEngineStatus()
            {
                // TODO: detect a sudden drop in oil pressure without triggering false positives caused by stalling the engine
                float averageOilTemp = oilTemp / samples;
                float averageWaterTemp = waterTemp / samples;
                float averageOilPressure = oilPressure / samples;
                if (averageOilTemp > maxSafeOilTemp && averageWaterTemp > maxSafeWaterTemp)
                {
                    return EngineStatus.HOT_OIL_AND_WATER;
                }
                else if (averageWaterTemp > maxSafeWaterTemp)
                {
                    return EngineStatus.HOT_WATER;
                }
                else if (averageOilTemp > maxSafeOilTemp)
                {
                    return EngineStatus.HOT_OIL;
                }
                else
                {
                    return EngineStatus.ALL_CLEAR;
                }
                // low oil pressure not (yet) implemented
            }
        }

        private enum EngineStatus
        {
            ALL_CLEAR, HOT_OIL, HOT_WATER, HOT_OIL_AND_WATER, LOW_OIL_PRESSURE
        }
    }
}
