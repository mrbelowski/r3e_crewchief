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

        private List<PushData> pushDataInFront;
        private List<PushData> pushDataBehind;
        private Boolean playedNearEndTimePush;
        private int previousDataWindowSizeToCheck = 2;
        private Boolean playedNearEndLapsPush;
        private Boolean drivingOutOfPits;

        public PushNow(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            pushDataInFront = new List<PushData>();
            pushDataBehind = new List<PushData>();
            playedNearEndTimePush = false;
            playedNearEndLapsPush = false;
            drivingOutOfPits = false;
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return true;
        }

        private float getOpponentBestLapInWindow(Boolean ahead)
        {
            float bestLap = -1;
            if (ahead)
            {
                for (int i = pushDataInFront.Count - 1; i > pushDataInFront.Count - previousDataWindowSizeToCheck; i--)
                {
                    float thisLap = pushDataInFront[i].lapTime + (pushDataInFront[i - 1].gap - pushDataInFront[i].gap);
                    if (bestLap == -1 || bestLap > thisLap)
                    {
                        bestLap = thisLap;
                    }
                }
            }
            else
            {
                for (int i = pushDataBehind.Count - 1; i > pushDataBehind.Count - previousDataWindowSizeToCheck; i--)
                {
                    float thisLap = pushDataBehind[i].lapTime - (pushDataBehind[i - 1].gap - pushDataBehind[i].gap);
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
            if (CommonData.isRaceStarted)
            {
                if (pushDataInFront == null || pushDataBehind == null)
                {
                    clearState();
                }
                if (CommonData.isNewLap)
                {
                    if (CommonData.racingSameCarInFront)
                    {
                        pushDataInFront.Add(new PushData(currentState.LapTimePrevious, currentState.TimeDeltaFront));
                    }
                    else
                    {
                        pushDataInFront.Clear();
                    }
                    if (CommonData.racingSameCarBehind)
                    {
                        pushDataBehind.Add(new PushData(currentState.LapTimePrevious, currentState.TimeDeltaBehind));
                    }
                    else
                    {
                        pushDataBehind.Clear();
                    }
                }
                if (currentState.NumberOfLaps == -1 && !playedNearEndTimePush && 
                        currentState.SessionTimeRemaining < 4 * 60 && currentState.SessionTimeRemaining > 2 * 60) 
                {
                    // estimate the number of remaining laps - be optimistic...
                    int numLapsLeft = (int)Math.Ceiling((double)currentState.SessionTimeRemaining / (double)currentState.LapTimeBest);
                    playedNearEndTimePush = checkGaps(currentState, numLapsLeft);
                }
                else if (currentState.NumberOfLaps > 0 && currentState.NumberOfLaps - currentState.CompletedLaps <= 4 && 
                    !playedNearEndLapsPush)
                {
                    playedNearEndLapsPush = checkGaps(currentState, currentState.NumberOfLaps - currentState.CompletedLaps);
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

        private Boolean checkGaps(Shared currentState, int numLapsLeft)
        {
            Boolean playedMessage = false;           
            if (currentState.Position > 1 && pushDataInFront.Count >= previousDataWindowSizeToCheck && 
                (getOpponentBestLapInWindow(true) - currentState.LapTimeBest) * numLapsLeft > currentState.TimeDeltaFront)
            {
                // going flat out, we're going to catch the guy ahead us before the end
                if (currentState.Position == 2)
                {
                    audioPlayer.queueClip(folderPushToGetWin, 0, this);
                }
                else if (currentState.Position == 3)
                {
                    audioPlayer.queueClip(folderPushToGetSecond, 0, this);
                }
                else if (currentState.Position == 4)
                {
                    audioPlayer.queueClip(folderPushToGetThird, 0, this);
                }
                else
                {
                    audioPlayer.queueClip(folderPushToImprove, 0, this);
                }
                playedMessage = true;
            }
            else if (!CommonData.isLast && pushDataBehind.Count >= previousDataWindowSizeToCheck && 
                (currentState.LapTimeBest - getOpponentBestLapInWindow(false)) * numLapsLeft > currentState.TimeDeltaBehind)
            {
                // even with us going flat out, the guy behind is going to catch us before the end
                audioPlayer.queueClip(folderPushToHoldPosition, 0, this);
                playedMessage = true;
            }
            return playedMessage;
        }

        private class PushData {
            public float lapTime;
            public float gap;

            public PushData(float lapTime, float gap)
            {
                this.lapTime = lapTime;
                this.gap = gap;
            }
        }
    }
}
