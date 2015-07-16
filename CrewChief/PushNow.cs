using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChief.Data;

namespace CrewChief.Events
{
    class PushNow : AbstractEvent
    {
        private String folderPushToImprove = "push_now/push_to_improve";
        private String folderPushToGetWin = "push_now/push_to_get_win";
        private String folderPushToGetSecond = "push_now/push_to_get_second";
        private String folderPushToGetThird = "push_now/push_to_get_third";
        private String folderPushToHoldPosition = "push_now/push_to_hold_position";

        private String folderPushExitingPits = "push_now/pits_exit_clear";
        private String folderTrafficBehindExitingPits = "push_now/pits_exit_traffic_behind";

        private List<PushData> pushData;
        private Boolean playedNearEndTimePush;
        private int previousDataWindowSizeToCheck = 2;
        private Boolean playedNearEndLapsPush;
        private Boolean drivingOutOfPits;

        public PushNow(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        protected override void clearStateInternal()
        {
            pushData = new List<PushData>();
            playedNearEndTimePush = false;
            playedNearEndLapsPush = false;
            playedExitPitsPush = false;
            drivingOutOfPits = false;
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return true;
        }

        private Boolean isPushDataValid(int currentPosition, int numCars)
        {
            if (pushData[pushData.Count - 1].position != currentPosition || 
                pushData[pushData.Count - 1].numCars != numCars)
            {
                return false;
            }
            Boolean pushDataValid = true;
            for (int i = pushData.Count - 1; i >= pushData.Count - previousDataWindowSizeToCheck; i--)
            {
                if (pushData[i].position != pushData[i-1].position || pushData[i].numCars != pushData[i-1].numCars)
                {
                    pushDataValid = false;
                    break;
                }
            }
            return pushDataValid;
        }

        private float getOpponentBestLapInWindow(Boolean ahead)
        {
            float bestLap = -1;
            for (int i = pushData.Count - 1; i >= pushData.Count - previousDataWindowSizeToCheck; i--)
            {
                if (ahead)
                {
                    float thisLap = pushData[i].lapTime + (pushData[i - 1].gapInFront - pushData[i].gapInFront);
                    if (bestLap == -1 || bestLap > thisLap)
                    {
                        bestLap = thisLap;
                    }
                }
                else
                {
                    float thisLap = pushData[i].lapTime - (pushData[i - 1].gapBehind - pushData[i].gapBehind);
                    if (bestLap == -1 || bestLap > thisLap)
                    {
                        bestLap = thisLap;
                    }
                }
            }
            return bestLap;
        }

        override protected void triggerInternal(Shared lastState, Shared currentState)
        {
            if (isRaceStarted) {
                if (pushData == null)
                {
                    clearStateInternal();
                }
                if (isNewLap)
                {
                    pushData.Add(new PushData(currentState.LapTimePrevious, currentState.TimeDeltaFront, 
                        currentState.TimeDeltaBehind, currentState.Position, currentState.NumCars));
                }
                if (currentState.NumberOfLaps == -1 && !playedNearEndTimePush && pushData.Count >= previousDataWindowSizeToCheck && 
                        currentState.SessionTimeRemaining < 4 * 60 && currentState.SessionTimeRemaining > 2 * 60) 
                {
                    if (isPushDataValid(currentState.Position, currentState.NumCars))
                    {
                        playedNearEndTimePush = true;
                        // estimate the number of remaining laps - be optimistic...
                        int numLapsLeft = (int)Math.Ceiling((double)currentState.SessionTimeRemaining / (double)currentState.LapTimeBest);
                        Console.WriteLine("Estimated number of laps left = " + numLapsLeft);
                        checkGaps(currentState, numLapsLeft);
                    }
                }
                else if (currentState.NumberOfLaps > 0 && currentState.NumberOfLaps - currentState.CompletedLaps <= 4 && 
                    !playedNearEndLapsPush && pushData.Count >= previousDataWindowSizeToCheck)
                {
                    if (isPushDataValid(currentState.Position, currentState.NumCars))
                    {
                        playedNearEndLapsPush = true;
                        checkGaps(currentState, currentState.NumberOfLaps - currentState.CompletedLaps);
                    }
                }
                else if (currentState.PitWindowStatus == (int)Constant.PitWindow.Completed && lastState.PitWindowStatus == (int)Constant.PitWindow.StopInProgress)
                {
                    drivingOutOfPits = true;
                }
                else if (drivingOutOfPits && currentState.ControlType == (int)Constant.Control.Player && lastState.ControlType == (int)Constant.Control.AI)
                {
                    drivingOutOfPits = false;
                    // we've just been handed control back after a pitstop
                    if (currentState.TimeDeltaFront > 3 && currentState.TimeDeltaBehind > 4)
                    {
                        // we've exited into clean air
                        audioPlayer.queueClip(folderPushExitingPits, 0, this);
                    }
                    else if (currentState.TimeDeltaBehind <= 4)
                    {
                        // we've exited the pits but there's traffic behind
                        audioPlayer.queueClip(folderTrafficBehindExitingPits, 0, this);
                    }
                }
            }
        }

        private void checkGaps(Shared currentState, int numLapsLeft)
        {
            Console.WriteLine("checking gaps...");
            if (currentState.Position > 1)
            {
                Console.WriteLine("before end, could gain " + ((getOpponentBestLapInWindow(true) - currentState.LapTimeBest) * numLapsLeft) + 
                    " current delta = " + currentState.TimeDeltaFront);
            }
            if (!isLast)
            {
                Console.WriteLine("before end, could lose " + ((currentState.LapTimeBest - getOpponentBestLapInWindow(false)) * numLapsLeft) +
                   " current delta = " + currentState.TimeDeltaBehind);
            }
            if (currentState.Position > 1 && (getOpponentBestLapInWindow(true) - currentState.LapTimeBest) * numLapsLeft > currentState.TimeDeltaFront)
            {
                // going flat out, we're going to catch the guy ahead us before the end
                if (currentState.Position == 2)
                {
                    audioPlayer.queueClip(folderPushToGetWin, 20, this);
                }
                else if (currentState.Position == 3)
                {
                    audioPlayer.queueClip(folderPushToGetSecond, 20, this);
                }
                else if (currentState.Position == 4)
                {
                    audioPlayer.queueClip(folderPushToGetThird, 20, this);
                }
                else
                {
                    audioPlayer.queueClip(folderPushToImprove, 20, this);
                }
            }
            else if (!isLast && (currentState.LapTimeBest - getOpponentBestLapInWindow(false)) * numLapsLeft > currentState.TimeDeltaBehind)
            {
                // even with us going flat out, the guy behind is going to catch us before the end
                audioPlayer.queueClip(folderPushToHoldPosition, 20, this);
            }   
        }

        private class PushData {
            public float lapTime;
            public float gapInFront;
            public float gapBehind;
            public int position;
            public int numCars;

            public PushData(float lapTime, float gapInFront, float gapBehind, int position, int numCars)
            {
                this.lapTime = lapTime;
                this.gapInFront = gapInFront;
                this.gapBehind = gapBehind;
                this.position = position;
                this.numCars = numCars;
            }
            public PushData(float gapInFront, float gapBehind, int position, int numCars)
            {
                this.gapInFront = gapInFront;
                this.gapBehind = gapBehind;
                this.position = position;
                this.numCars = numCars;
            }
        }
    }
}
