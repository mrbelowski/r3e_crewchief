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

        // if the lap is within 0.5% of the best lap time play a message
        private Single goodLapPercent = 0.5f;

        public LapTimes(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        protected override void clearStateInternal()
        {
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return true;
        }

        protected override void triggerInternal(Data.Shared lastState, Data.Shared currentState)
        {
            if (isNewLap && currentState.CompletedLaps > 2)
            {
                if (currentState.LapTimePrevious > 0 && currentState.LapTimePrevious <= currentState.LapTimeBest)
                {
                    audioPlayer.queueClip(folderBestLap, 0, this);
                }
                else if (currentState.LapTimePrevious - (currentState.LapTimePrevious * goodLapPercent / 100) < currentState.LapTimeBest)
                {
                    audioPlayer.queueClip(folderGoodLap, 0, this);                    
                }
            }
        }
    }
}
