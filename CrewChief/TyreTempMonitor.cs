using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChief.Data;

namespace CrewChief.Events
{
    class TyreTempMonitor : AbstractEvent
    {
        private String folderHotLeftFront = "tyre_temp_monitor/hot_left_front";
        private String folderHotLeftRear = "tyre_temp_monitor/hot_left_rear";
        private String folderHotRightFront = "tyre_temp_monitor/hot_right_front";
        private String folderHotRightRear = "tyre_temp_monitor/hot_right_rear";
        private String folderHotFronts = "tyre_temp_monitor/hot_fronts";
        private String folderHotRears = "tyre_temp_monitor/hot_rears";
        private String folderHotLefts = "tyre_temp_monitor/hot_lefts";
        private String folderHotRights = "tyre_temp_monitor/hot_rights";
        private String folderHotAllRound = "tyre_temp_monitor/hot_all_round";
        private String folderGoodTemps = "tyre_temp_monitor/good_temps";

        private static float maxColdTemp = Properties.Settings.Default.max_cold_tyre_temp;
        private static float maxGoodTemp = Properties.Settings.Default.max_good_tyre_temp;

        // play these messages 10 seconds after crossing the line
        private int tyreTempMessageDelay = 10;

        private TyreTemps lastLapTyreTemps;
        private TyreTemps thisLapTyreTemps;

        private Boolean hasPlayedMessage;

        private TyreTempStatus lastStatus;

        public TyreTempMonitor(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        protected override void clearStateInternal()
        {
            lastLapTyreTemps = new TyreTemps();
            thisLapTyreTemps = new TyreTemps();
            lastStatus = TyreTempStatus.COLD;
            hasPlayedMessage = false;
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return isSessionRunning;
        }
        
        override protected void triggerInternal(Shared lastState, Shared currentState)
        {
            if (isNewLap && isSessionRunning)
            {                
                if (currentState.CompletedLaps > 1)
                {
                    if (lastLapTyreTemps == null)
                    {
                        clearStateInternal();
                        updateTyreTemps(currentState, lastLapTyreTemps);
                    }
                    lastLapTyreTemps.display();
                    // completed 2 laps, so now we have some comparison data
                    TyreTempStatus tempStatus = lastLapTyreTemps.getStatus();
                    if (tempStatus != lastStatus || !hasPlayedMessage)
                    {
                        hasPlayedMessage = true;
                        switch (tempStatus)
                        {
                            case TyreTempStatus.GOOD:
                                audioPlayer.queueClip(folderGoodTemps, tyreTempMessageDelay, this);
                                break;
                            case TyreTempStatus.HOT_ALL_ROUND:
                                audioPlayer.queueClip(folderHotAllRound, tyreTempMessageDelay, this);
                                break;
                            case TyreTempStatus.HOT_FRONTS:
                                audioPlayer.queueClip(folderHotFronts, tyreTempMessageDelay, this);
                                break;
                            case TyreTempStatus.HOT_REARS:
                                audioPlayer.queueClip(folderHotRears, tyreTempMessageDelay, this);
                                break;
                            case TyreTempStatus.HOT_LEFTS:
                                audioPlayer.queueClip(folderHotLefts, tyreTempMessageDelay, this);
                                break;
                            case TyreTempStatus.HOT_RIGHTS:
                                audioPlayer.queueClip(folderHotRights, tyreTempMessageDelay, this);
                                break;
                            case TyreTempStatus.HOT_LEFT_FRONT:
                                audioPlayer.queueClip(folderHotLeftFront, tyreTempMessageDelay, this);
                                break;
                            case TyreTempStatus.HOT_LEFT_REAR:
                                audioPlayer.queueClip(folderHotLeftRear, tyreTempMessageDelay, this);
                                break;
                            case TyreTempStatus.HOT_RIGHT_FRONT:
                                audioPlayer.queueClip(folderHotRightFront, tyreTempMessageDelay, this);
                                break;
                            case TyreTempStatus.HOT_RIGHT_REAR:
                                audioPlayer.queueClip(folderHotRightRear, tyreTempMessageDelay, this);
                                break;
                            case TyreTempStatus.COLD:
                                Console.WriteLine("tyres are cold");
                                break;
                        }
                        lastStatus = tempStatus;
                    }
                }
                lastLapTyreTemps = thisLapTyreTemps;
                thisLapTyreTemps = new TyreTemps();
            }
            if (thisLapTyreTemps == null)
            {
                Console.WriteLine("resetting tyre temps data");
                clearStateInternal();
            }
            updateTyreTemps(currentState, thisLapTyreTemps);
        }

        private void updateTyreTemps(Shared data, TyreTemps tyreTemps) {            
            tyreTemps.addSample((data.TireTemp.FrontLeft_Left + data.TireTemp.FrontLeft_Center + data.TireTemp.FrontLeft_Right) / 3,
                (data.TireTemp.FrontRight_Left + data.TireTemp.FrontRight_Center + data.TireTemp.FrontRight_Right) / 3,
                (data.TireTemp.RearLeft_Left + data.TireTemp.RearLeft_Center + data.TireTemp.RearLeft_Right) / 3,
                (data.TireTemp.RearRight_Left + data.TireTemp.RearRight_Center + data.TireTemp.RearRight_Right) / 3);
        }

        private class TyreTemps
        {
            // these are the average temp over a single lap
            private float totalLeftFrontTemp = 0;
            private float totalRightFrontTemp = 0;
            private float totalLeftRearTemp = 0;
            private float totalRightRearTemp = 0;
            private int tyreTempSamples = 0;
            public TyreTemps()
            {

            }
            public void display()
            {
                Console.WriteLine("Temps status: " + getStatus() + "\nleft front: " + getLeftFrontAverage() + " right front: " + getRightFrontAverage() +
                    "\nleft rear: " + getLeftRearAverage() + " right rear: " + getRightRearAverage());
            }
            public void addSample(float leftFrontTemp, float rightFrontTemp, float leftRearTemp, float rightRearTemp)
            {
                tyreTempSamples++;
                totalLeftFrontTemp += leftFrontTemp;
                totalRightFrontTemp += rightFrontTemp;
                totalLeftRearTemp += leftRearTemp;
                totalRightRearTemp += rightRearTemp;
            }
            public float getLeftFrontAverage()
            {
                if (tyreTempSamples == 0)
                {
                    return 0;
                }
                return totalLeftFrontTemp / tyreTempSamples;
            }
            public float getLeftRearAverage()
            {
                if (tyreTempSamples == 0)
                {
                    return 0;
                }
                return totalLeftRearTemp / tyreTempSamples;
            }
            public float getRightFrontAverage()
            {
                if (tyreTempSamples == 0)
                {
                    return 0;
                }
                return totalRightFrontTemp / tyreTempSamples;
            }
            public float getRightRearAverage()
            {
                if (tyreTempSamples == 0)
                {
                    return 0;
                }
                return totalRightRearTemp / tyreTempSamples;
            }
            public TyreTempStatus getStatus()
            {
                if (getLeftFrontAverage() < maxColdTemp && getLeftRearAverage() < maxColdTemp &&
                    getRightFrontAverage() < maxColdTemp && getRightRearAverage() < maxColdTemp)
                {
                    return TyreTempStatus.COLD;
                }
                if (getLeftFrontAverage() > maxGoodTemp && getLeftRearAverage() > maxGoodTemp &&
                    getRightFrontAverage() > maxGoodTemp && getRightRearAverage() > maxGoodTemp)
                {
                    return TyreTempStatus.HOT_ALL_ROUND;
                } 
                else if (getLeftFrontAverage() > maxGoodTemp && getRightFrontAverage() > maxGoodTemp)
                {
                    return TyreTempStatus.HOT_FRONTS;
                }
                else if (getLeftRearAverage() > maxGoodTemp && getRightRearAverage() > maxGoodTemp)
                {
                    return TyreTempStatus.HOT_REARS;
                }
                else if (getLeftFrontAverage() > maxGoodTemp && getLeftRearAverage() > maxGoodTemp)
                {
                    return TyreTempStatus.HOT_LEFTS;
                }
                else if (getRightFrontAverage() > maxGoodTemp && getRightRearAverage() > maxGoodTemp)
                {
                    return TyreTempStatus.HOT_RIGHTS;
                }
                else if (getLeftFrontAverage() > maxGoodTemp)
                {
                    return TyreTempStatus.HOT_LEFT_FRONT;
                }
                else if (getLeftRearAverage() > maxGoodTemp)
                {
                    return TyreTempStatus.HOT_LEFT_REAR;
                }
                else if (getRightFrontAverage() > maxGoodTemp)
                {
                    return TyreTempStatus.HOT_RIGHT_FRONT;
                }
                else if (getRightRearAverage() > maxGoodTemp)
                {
                    return TyreTempStatus.HOT_RIGHT_REAR;
                }
                else
                {
                    return TyreTempStatus.GOOD;
                }
            }
        }

        private enum TyreTempStatus
        {
            HOT_LEFT_FRONT, HOT_RIGHT_FRONT, HOT_LEFT_REAR, HOT_RIGHT_REAR,
            HOT_FRONTS, HOT_REARS, HOT_LEFTS, HOT_RIGHTS, HOT_ALL_ROUND, GOOD, COLD
        }
        
    }
}
