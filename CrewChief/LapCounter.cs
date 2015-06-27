using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChief.Data;

namespace CrewChief.Events
{
    class LapCounter : AbstractEvent
    {
        private String folderLastLap = "lap_counter/last_lap";
    
        private String folderTwoLeft = "lap_counter/two_to_go";
    
        private String folderLastLapLeading = "lap_counter/last_lap_leading";
    
        private String folderLastLapTopThree = "lap_counter/last_lap_top_three";
    
        private String folderTwoLeftLeading = "lap_counter/two_to_go_leading";

        private String folderTwoLeftTopThree = "lap_counter/two_to_go_top_three";

        private String folderPodiumFinish = "lap_counter/podium_finish";

        private String folderWonRace = "lap_counter/won_race";

        private String folderFinishedRace = "lap_counter/finished_race";
    
        public LapCounter(AudioPlayer audioPlayer) {
            this.audioPlayer = audioPlayer;
        }

        protected override void clearStateInternal()
        {
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return true;
        }
        
        override protected void triggerInternal(Shared lastState, Shared currentState)
        {
            if (isRaceMode && isNewLap) {
                // a new lap has been started in race mode

                // Note this will only trigger for DTM races with a number of laps - non DTM races are
                // timed and there's no data about the race time in the memory block
                if (currentState.CompletedLaps == currentState.NumberOfLaps && currentState.NumPenalties < 1) {
                    if (currentState.Position == 1)
                    {
                        audioPlayer.queueClip(folderWonRace, 0, this);
                    }
                    else if (currentState.Position < 4)
                    {
                        audioPlayer.queueClip(folderPodiumFinish, 0, this);
                    }
                    else
                    {
                        audioPlayer.queueClip(folderFinishedRace, 0, this);
                    }
                }
                else if (currentState.CompletedLaps == currentState.NumberOfLaps - 1) {
                    if (currentState.Position == 1) {
                        audioPlayer.queueClip(folderLastLapLeading, 0, this);
                    } else if (currentState.Position < 4) {
                        audioPlayer.queueClip(folderLastLapTopThree, 0, this);
                    } else {
                        audioPlayer.queueClip(folderLastLap, 0, this);
                    }
                } else if (currentState.CompletedLaps == currentState.NumberOfLaps - 2) {
                    if (currentState.Position == 1) {
                        audioPlayer.queueClip(folderTwoLeftLeading, 0, this);
                    } else if (currentState.Position < 4) {
                        audioPlayer.queueClip(folderTwoLeftTopThree, 0, this);
                    } else {
                        audioPlayer.queueClip(folderTwoLeft, 0, this);
                    }
                } 
            }
        }  
    }
}
