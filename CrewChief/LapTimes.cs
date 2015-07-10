using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChief.Events
{
    class LapTimes : AbstractEvent
    {
        private String folderBestLap = "lap_times/best_lap";

        private String folderGoodLap = "lap_times/good_lap";

        private String folderConsistentTimes = "lap_times/consistent";

        private String folderImprovingTimes = "lap_times/improving";

        private String folderWorseningTimes = "lap_times/worsening";

        // if the lap is within 0.5% of the best lap time play a message
        private Single goodLapPercent = 0.5f;

        // if the lap is within 1% of the previous lap it's considered consistent
        private Single consistencyLimit = 1f;

        private List<float> lapTimesWindow;

        private int lapTimesWindowSize = 3;

        private float bestLapTime;

        private ConsistencyResult lastConsistencyMessage;

        // lap number when the last consistency update was made
        private int lastConsistencyUpdate;

        private Boolean lapIsValid;

        public LapTimes(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        protected override void clearStateInternal()
        {
            lapTimesWindow = new List<float>(lapTimesWindowSize);
            bestLapTime = 0;
            lastConsistencyUpdate = 0;
            lastConsistencyMessage = ConsistencyResult.NOT_APPLICABLE;
            lapIsValid = true;
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return isSessionRunning;
        }

        protected override void triggerInternal(Data.Shared lastState, Data.Shared currentState)
        {
            // in race sessions (race only) the previousLapTime isn't set to -1 if that lap was invalid, so 
            // we need to record that it's invalid while we're actually on the lap
            if (isSessionRunning && lapIsValid && currentState.CompletedLaps > 0 && !isNewLap && currentState.LapTimeCurrent == -1)
            {
                lapIsValid = false;
            }
            if (isSessionRunning && isNewLap && currentState.CompletedLaps > 0)
            {
                if (lapTimesWindow == null)
                {
                    lapTimesWindow = new List<float>(lapTimesWindowSize);
                }

                lapTimesWindow.Insert(0, currentState.LapTimePrevious);
                if (currentState.LapTimePrevious > 0 && currentState.LapTimePrevious < bestLapTime)
                {
                    bestLapTime = currentState.LapTimePrevious;
                }
                if (currentState.CompletedLaps > 2)
                {
                    if (lapIsValid && currentState.LapTimePrevious > 0 && currentState.LapTimePrevious <= currentState.LapTimeBest)
                    {
                        audioPlayer.queueClip(folderBestLap, 0, this, PearlsOfWisdom.PearlType.GOOD, 0.2);
                    }
                    else if (currentState.CompletedLaps >= lastConsistencyUpdate + lapTimesWindowSize && lapTimesWindow.Count >= lapTimesWindowSize)
                    {
                        ConsistencyResult consistency = checkAgainstPreviousLaps();
                        if (consistency == ConsistencyResult.CONSISTENT)
                        {
                            lastConsistencyUpdate = currentState.CompletedLaps;
                            audioPlayer.queueClip(folderConsistentTimes, 0, this);
                        }
                        else if (consistency == ConsistencyResult.IMPROVING)
                        {
                            lastConsistencyUpdate = currentState.CompletedLaps;
                            audioPlayer.queueClip(folderImprovingTimes, 0, this);
                        }
                        if (consistency == ConsistencyResult.WORSENING)
                        {
                            lastConsistencyUpdate = currentState.CompletedLaps;
                            audioPlayer.queueClip(folderWorseningTimes, 0, this);
                        }
                    }
                    else if (lapIsValid && currentState.LapTimePrevious > 0 && currentState.LapTimePrevious - (currentState.LapTimePrevious * goodLapPercent / 100) < currentState.LapTimeBest)
                    {
                        audioPlayer.queueClip(folderGoodLap, 0, this);
                    }                    
                }
                lapIsValid = true;
            }
        }

        private ConsistencyResult checkAgainstPreviousLaps()
        {
            Boolean isImproving = true;
            Boolean isWorsening = true;
            Boolean isConsistent = true;

            for (int index = 0; index < lapTimesWindowSize - 1; index++)
            {
                // check the lap time was recorded
                if (lapTimesWindow[index] <= 0)
                {
                    Console.WriteLine("no data for consistency check");
                    lastConsistencyMessage = ConsistencyResult.NOT_APPLICABLE;
                    return ConsistencyResult.NOT_APPLICABLE;
                }
                if (lapTimesWindow[index] >= lapTimesWindow[index + 1])
                {
                    isImproving = false;
                    break;
                }
            }

            for (int index = 0; index < lapTimesWindowSize - 1; index++)
            {
                if (lapTimesWindow[index] <= lapTimesWindow[index + 1])
                {
                    isWorsening = false;
                }
            }

            for (int index = 0; index < lapTimesWindowSize - 1; index++)
            {
                float lastLap = lapTimesWindow[index];
                float lastButOneLap = lapTimesWindow[index + 1];
                float consistencyRange = (lastButOneLap * consistencyLimit) / 100;
                if (lastLap > lastButOneLap + consistencyRange || lastLap < lastButOneLap - consistencyRange)
                {
                    isConsistent = false;
                }
            }

            // todo: untangle this mess....
            if (isImproving)
            {
                if (lastConsistencyMessage == ConsistencyResult.IMPROVING)
                {
                    // don't play the same improving message - see if the consistent message might apply
                    if (isConsistent)
                    {
                        lastConsistencyMessage = ConsistencyResult.CONSISTENT;
                        return ConsistencyResult.CONSISTENT;
                    }
                }
                else
                {
                    lastConsistencyMessage = ConsistencyResult.IMPROVING;
                    return ConsistencyResult.IMPROVING;
                }
            }
            if (isWorsening)
            {
                if (lastConsistencyMessage == ConsistencyResult.WORSENING)
                {
                    // don't play the same worsening message - see if the consistent message might apply
                    if (isConsistent)
                    {
                        lastConsistencyMessage = ConsistencyResult.CONSISTENT;
                        return ConsistencyResult.CONSISTENT;
                    }
                }
                else
                {
                    lastConsistencyMessage = ConsistencyResult.WORSENING;
                    return ConsistencyResult.WORSENING;
                }
            }
            if (isConsistent)
            {
                lastConsistencyMessage = ConsistencyResult.CONSISTENT;
                return ConsistencyResult.CONSISTENT;
            }
            return ConsistencyResult.NOT_APPLICABLE;
        }

        private enum ConsistencyResult
        {
            NOT_APPLICABLE, CONSISTENT, IMPROVING, WORSENING
        }
    }
}
