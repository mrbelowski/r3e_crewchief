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

        public LapTimes(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        protected override void clearStateInternal()
        {
            initialiseLapTimesWindow();
            bestLapTime = 0;
            lastConsistencyUpdate = 0;
            lastConsistencyMessage = ConsistencyResult.NOT_APPLICABLE;
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return true;
        }

        protected override void triggerInternal(Data.Shared lastState, Data.Shared currentState)
        {
            // race is ended check - only works for DTM at the moment :(
            if (isNewLap && currentState.NumberOfLaps > 0 && currentState.CompletedLaps == currentState.NumberOfLaps)
            {
                return;
            }
            if (isNewLap && currentState.CompletedLaps > 0)
            {
                if (lapTimesWindow == null || lapTimesWindow.Count == 0)
                {
                    initialiseLapTimesWindow();
                }
                else
                {
                    for (int i = lapTimesWindow.Count - 1; i > 0; i--)
                    {
                        lapTimesWindow[i] = lapTimesWindow[i - 1];
                    }
                }
                
                lapTimesWindow[0] = currentState.LapTimePrevious;
                if (currentState.LapTimePrevious < bestLapTime)
                {
                    bestLapTime = currentState.LapTimePrevious;
                }
                if (currentState.CompletedLaps > 2)
                {
                    if (currentState.LapTimePrevious > 0 && currentState.LapTimePrevious <= currentState.LapTimeBest)
                    {
                        audioPlayer.queueClip(folderBestLap, 0, this, PearlsOfWisdom.PearlType.GOOD, 0.5);
                    }
                    else if (currentState.CompletedLaps >= lastConsistencyUpdate + lapTimesWindowSize)
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
                            audioPlayer.queueClip(folderWorseningTimes, 0, this, PearlsOfWisdom.PearlType.BAD, 0.5);
                        }
                    }
                    else if (currentState.LapTimePrevious - (currentState.LapTimePrevious * goodLapPercent / 100) < currentState.LapTimeBest)
                    {
                        audioPlayer.queueClip(folderGoodLap, 0, this);
                    }                    
                }
            }
        }

        private ConsistencyResult checkAgainstPreviousLaps()
        {
            Boolean isImproving = true;
            Boolean isWorsening = true;
            Boolean isConsistent = true;

            for (int index = 0; index < lapTimesWindow.Count - 1; index++)
            {
                // belt n braces - shouldn't end up in here without a list full of data...
                if (lapTimesWindow[index] == 0)
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
            
            for (int index = 0; index < lapTimesWindow.Count - 1; index++)
            {
                if (lapTimesWindow[index] <= lapTimesWindow[index + 1])
                {
                    isWorsening = false;
                }
            }

            for (int index = 0; index < lapTimesWindow.Count - 1; index++)
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

        private void initialiseLapTimesWindow()
        {
            lapTimesWindow = new List<float>(lapTimesWindowSize);
            for (int i = 0; i < lapTimesWindowSize; i++)
            {
                lapTimesWindow.Add(0.0f);
            }
        }

        private enum ConsistencyResult
        {
            NOT_APPLICABLE, CONSISTENT, IMPROVING, WORSENING
        }
    }
}
