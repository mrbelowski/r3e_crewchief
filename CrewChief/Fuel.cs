using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChief.Data;

namespace CrewChief.Events
{
    class Fuel : AbstractEvent
    {
        private String folderOneLapEstimate = "fuel/one_lap_fuel";

        private String folderTwoLapsEstimate = "fuel/two_laps_fuel";

        private String folderThreeLapsEstimate = "fuel/three_laps_fuel";

        private String folderFourLapsEstimate = "fuel/four_laps_fuel";

        private String folderHalfDistanceGoodFuel = "fuel/half_distance_good_fuel";

        private String folderHalfDistanceLowFuel = "fuel/half_distance_low_fuel";

        private String folderHalfTankWarning = "fuel/half_tank_warning";

        private String folderTenMinutesFuel = "fuel/ten_minutes_fuel";

        private String folderTwoMinutesFuel = "fuel/two_minutes_fuel";

        private String folderFiveMinutesFuel = "fuel/five_minutes_fuel";

        private float averageUsagePerLap;

        private float averageUsagePerMinute;

        // fuel in tank 15 seconds after game start
        private float fuelAfter15Seconds;
        
        private int halfDistance;

        private float halfTime;

        private Boolean playedThreeQuarterTankWarning;

        private Boolean playedHalfTankWarning;

        private Boolean playedQuarterTankWarning;

        private Boolean initialised;

        private Boolean playedHalfTimeFuelEstimate;

        private int fuelUseWindowLength = 3;

        private List<float> fuelUseWindow;

        private double gameTimeAtLastFuelWindowUpdate;

        private Boolean playedTwoMinutesRemaining;

        private Boolean playedFiveMinutesRemaining;

        private Boolean playedTenMinutesRemaining;

        // check fuel use every 2 minutes
        private int fuelUseSampleTime = 2;

        public Fuel(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        protected override void clearStateInternal()
        {
            fuelAfter15Seconds = 0;
            averageUsagePerLap = 0;
            halfDistance = 0;
            playedThreeQuarterTankWarning = false;
            playedHalfTankWarning = false;
            playedQuarterTankWarning = false;
            initialised = false;
            halfTime = 0;
            playedHalfTimeFuelEstimate = false;
            fuelUseWindow = new List<float>();
            gameTimeAtLastFuelWindowUpdate = 0;
            averageUsagePerMinute = 0;
            playedFiveMinutesRemaining = false;
            playedTenMinutesRemaining = false;
            playedTwoMinutesRemaining = false;
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return isSessionRunning;
        }

        override protected void triggerInternal(Shared lastState, Shared currentState)
        {
            if (isRaceStarted && currentState.FuelUseActive == 1)
            {
                // To get the initial fuel, wait for 15 seconds
                if (!initialised && currentState.Player.GameSimulationTime > 15)
                {
                    fuelAfter15Seconds = currentState.FuelLeft;
                    fuelUseWindow.Add(fuelAfter15Seconds);
                    gameTimeAtLastFuelWindowUpdate = currentState.Player.GameSimulationTime;
                    Console.WriteLine("Fuel after 15s = " + fuelAfter15Seconds);
                    initialised = true;
                    if (currentState.NumberOfLaps > 0)
                    {
                        halfDistance = currentState.NumberOfLaps / 2;
                    }
                    else
                    {
                        halfTime = currentState.SessionTimeRemaining / 2;
                    }
                }
                if (isNewLap && initialised && currentState.CompletedLaps > 0 && currentState.NumberOfLaps > 0)
                {
                    // completed a lap, so store the fuel left at this point:
                    fuelUseWindow.Add(currentState.FuelLeft);
                    // if we've got fuelUseWindowLength + 1 samples (note we initialise the window data with fuelAt15Seconds so we always
                    // have one extra), get the average difference between each pair of values

                    // only do this if we have a full window of data + one extra start point
                    if (fuelUseWindow.Count > fuelUseWindowLength)
                    {
                        averageUsagePerLap = 0;
                        for (int i = fuelUseWindow.Count - 1; i > fuelUseWindow.Count - fuelUseWindowLength; i-- )
                        {
                            averageUsagePerLap += (fuelUseWindow[i] - fuelUseWindow[i-1]);
                        }
                        averageUsagePerLap = averageUsagePerLap / fuelUseWindowLength;
                    }
                    else
                    {
                        averageUsagePerLap = (fuelAfter15Seconds - currentState.FuelLeft) / currentState.CompletedLaps;
                    }
                    int estimatedFuelLapsLeft = (int)Math.Floor(currentState.FuelLeft / averageUsagePerLap);                        
                    if (currentState.CompletedLaps == halfDistance)
                    {
                        if (estimatedFuelLapsLeft > halfDistance)
                        {
                            audioPlayer.queueClip(folderHalfDistanceGoodFuel, 0, this);
                        }
                        else
                        {
                            audioPlayer.queueClip(folderHalfDistanceLowFuel, 0, this);
                        }
                    }
                    else if (estimatedFuelLapsLeft == 4)
                    {
                        Console.WriteLine("4 laps fuel left, starting fuel = " + fuelAfter15Seconds +
                                ", current fuel = " + currentState.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                        audioPlayer.queueClip(folderFourLapsEstimate, 0, this);
                    }
                    else if (estimatedFuelLapsLeft == 3)
                    {
                        Console.WriteLine("3 laps fuel left, starting fuel = " + fuelAfter15Seconds +
                            ", current fuel = " + currentState.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                        audioPlayer.queueClip(folderThreeLapsEstimate, 0, this);
                    }
                    else if (estimatedFuelLapsLeft == 2)
                    {
                        Console.WriteLine("2 laps fuel left, starting fuel = " + fuelAfter15Seconds +
                            ", current fuel = " + currentState.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                        audioPlayer.queueClip(folderTwoLapsEstimate, 0, this);
                    }
                    else if (estimatedFuelLapsLeft == 1)
                    {
                        Console.WriteLine("1 lap fuel left, starting fuel = " + fuelAfter15Seconds +
                            ", current fuel = " + currentState.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                        audioPlayer.queueClip(folderOneLapEstimate, 0, this);
                    }
                }
                else if (initialised && currentState.NumberOfLaps < 0 && currentState.Player.GameSimulationTime > gameTimeAtLastFuelWindowUpdate + (60 * fuelUseSampleTime)) 
                {
                    // it's 2 minutes since the last fuel window check
                    gameTimeAtLastFuelWindowUpdate = currentState.Player.GameSimulationTime;
                    fuelUseWindow.Add(currentState.FuelLeft);
                    // if we've got fuelUseWindowLength + 1 samples (note we initialise the window data with fuelAt15Seconds so we always
                    // have one extra), get the average difference between each pair of values

                    // only do this if we have a full window of data + one extra start point
                    if (fuelUseWindow.Count > fuelUseWindowLength)
                    {
                        averageUsagePerMinute = 0;
                        for (int i = fuelUseWindow.Count - 1; i > fuelUseWindow.Count - fuelUseWindowLength; i-- )
                        {
                            averageUsagePerMinute += (fuelUseWindow[i] - fuelUseWindow[i-1]);
                        }
                        averageUsagePerMinute = averageUsagePerMinute / (fuelUseWindowLength * fuelUseSampleTime);
                    }
                    else
                    {
                        averageUsagePerMinute = 60 * (fuelAfter15Seconds - currentState.FuelLeft) / (float) gameTimeAtLastFuelWindowUpdate;
                    }
                    int estimatedFuelMinutesLeft = (int)Math.Floor(currentState.FuelLeft / averageUsagePerMinute);

                    if (!playedHalfTimeFuelEstimate && currentState.SessionTimeRemaining < halfTime)
                    {
                        playedHalfTimeFuelEstimate = true;
                        if (averageUsagePerMinute * halfTime < currentState.FuelLeft) 
                        {
                            audioPlayer.queueClip(folderHalfDistanceLowFuel, 0, this);
                        }
                        else
                        {
                            audioPlayer.queueClip(folderHalfDistanceGoodFuel, 0, this);
                        }
                    }
                    else if (currentState.FuelLeft / averageUsagePerMinute < 2 && !playedTwoMinutesRemaining) {
                        playedTwoMinutesRemaining = true;
                        playedFiveMinutesRemaining = true;
                        playedTenMinutesRemaining = true;
                        audioPlayer.queueClip(folderTwoMinutesFuel, 0, this);
                    }
                    else if (currentState.FuelLeft / averageUsagePerMinute < 5 && !playedFiveMinutesRemaining) {
                        playedFiveMinutesRemaining = true;
                        playedTenMinutesRemaining = true;
                        audioPlayer.queueClip(folderFiveMinutesFuel, 0, this);
                    }
                    else if (currentState.FuelLeft / averageUsagePerMinute < 10 && !playedTenMinutesRemaining) {
                        playedTenMinutesRemaining = true;
                        audioPlayer.queueClip(folderTenMinutesFuel, 0, this);
                    }
                }
                else if (initialised && !playedHalfTankWarning && currentState.FuelLeft / fuelAfter15Seconds <= 0.50)
                {
                    // warning message for fuel left - these play as soon as the fuel reaches 1/2 tank left
                    playedHalfTankWarning = true;
                    audioPlayer.queueClip(folderHalfTankWarning, 0, this);
                }
            }
        }
    }
}
