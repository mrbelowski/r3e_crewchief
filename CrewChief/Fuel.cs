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

        private String folderQuarterTankWarning = "fuel/quarter_tank_warning";

        private String folderHalfTankWarning = "fuel/half_tank_warning";

        private String folderThreeQuarterTankWarning = "fuel/three_quarter_tank_warning";

        private float averageUsagePerLap;

        // fuel in tank 15 seconds after game start
        private float fuelAfter15Seconds;
        
        private int halfDistance;

        private float halfTime;

        private Boolean playedThreeQuarterTankWarning;

        private Boolean playedHalfTankWarning;

        private Boolean playedQuarterTankWarning;

        private Boolean initialised;

        private Boolean playedHalfTimeFuelEstimate;

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
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return true;
        }

        override protected void triggerInternal(Shared lastState, Shared currentState)
        {
            if (isRaceStarted && currentState.FuelUseActive == 1)
            {
                // some nasty logic here. When starting a race with fuel use disabled it starts out at 100L and quickly changes to 50L, then no more usage
                // To get the initial fuel, wait for the race to actually start - use 15 seconds here - then check again after another 15
                if (!initialised && currentState.Player.GameSimulationTime > 15)
                {
                    fuelAfter15Seconds = currentState.FuelLeft;
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
                if (isNewLap && initialised)
                {
                    // in races where we know how many laps there are (DTM 2014 only AFAIK), 
                    // we can check fuel usage at half distance and say whether the player needs to 
                    // save fuel in the 2nd half of the race. Of course, DTM 2014 doesn't include fuel
                    // usage, and races with fuel usage are (I think) timed rather than a number of laps,
                    // and the race time & time left aren't in the shared memory block, so this block is unreachable :(
                    if (currentState.FuelUseActive == 1 && currentState.CompletedLaps > 0 && currentState.NumberOfLaps > 0)
                    {
                        averageUsagePerLap = (fuelAfter15Seconds - currentState.FuelLeft) / currentState.CompletedLaps;

                        int estimatedFuelLapsLeft = (int)Math.Floor(currentState.FuelLeft / averageUsagePerLap);
                        
                        if (currentState.CompletedLaps == halfDistance)
                        {
                            if (currentState.FuelLeft > fuelAfter15Seconds / 2)
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
                }
                else if (!playedHalfTimeFuelEstimate && currentState.SessionTimeRemaining < halfTime)
                {
                    playedHalfTimeFuelEstimate = true;
                    if (currentState.FuelLeft / fuelAfter15Seconds <= 0.50)
                    {
                        audioPlayer.queueClip(folderHalfDistanceLowFuel, 0, this);
                    }
                    else
                    {
                        audioPlayer.queueClip(folderHalfDistanceGoodFuel, 0, this);
                    }
                }
                else
                {
                    if (currentState.FuelUseActive == 1)
                    {
                        // warning messages for fuel left - these play as soon as the fuel reaches 
                        // 3/4, 1/2, or 1/4 of a tank left
                        if (!playedQuarterTankWarning && currentState.FuelLeft / fuelAfter15Seconds <= 0.25)
                        {
                            // if we're playing this message we don't want to play the 1/2 and 3/4 tank
                            playedQuarterTankWarning = true;
                            playedHalfTankWarning = true;
                            playedThreeQuarterTankWarning = true;
                            audioPlayer.queueClip(folderQuarterTankWarning, 0, this);
                        }
                        else if (!playedHalfTankWarning && currentState.FuelLeft / fuelAfter15Seconds <= 0.50)
                        {
                            playedHalfTankWarning = true;
                            playedThreeQuarterTankWarning = true;
                            audioPlayer.queueClip(folderHalfTankWarning, 0, this);
                        }
                        else if (!playedThreeQuarterTankWarning && currentState.FuelLeft / fuelAfter15Seconds <= 0.75)
                        {
                            playedThreeQuarterTankWarning = true;
                            audioPlayer.queueClip(folderThreeQuarterTankWarning, 0, this);
                        }
                    }
                }
            }
        }
    }
}
