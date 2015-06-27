using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChief.Data;

namespace CrewChief.Events
{
    class Penalties : AbstractEvent
    {
        // time (in seconds) to delay messages about penalty laps to go - 
        // we need this because the play might cross the start line while serving 
        // a penalty, so we should wait before telling them how many laps they have to serve it
        private int pitstopDelay = 20;
    
        private String folderNewPenalty = "penalties/new_penalty";

        private String folderThreeLapsToServe = "penalties/penalty_three_laps_left";

        private String folderTwoLapsToServe = "penalties/penalty_two_laps_left";
            
        private String folderOneLapToServe = "penalties/penalty_one_lap_left";

        private String folderDisqualified = "penalties/penalty_disqualified";
            
        private int penaltyLap;

        private int lapsCompleted;

        private Boolean hasOutstandingPenalty = false;

        public Penalties(AudioPlayer audioPlayer) {
            this.audioPlayer = audioPlayer;
        }

        protected override void clearStateInternal()
        {
            penaltyLap = -1;
            lapsCompleted = -1;
            hasOutstandingPenalty = false;
            // edge case here: if a penalty is given and immediately served (slow down penalty), then
            // the player gets another within the next 20 seconds, the 'you have 3 laps to come in to serve'
            // message would be in the queue and would be made valid again, so would play. So we explicity 
            // remove this message from the queue
            audioPlayer.removeQueuedClip(folderThreeLapsToServe);
        }

        public override bool isClipStillValid(string eventSubType)
        {
            // when a new penalty is given we queue a 'three laps left to serve' message for 20 seconds in the future.
            // If, 20 seconds later, the player has started a new lap, this message is no longer valid so shouldn't be played
            if (eventSubType == folderThreeLapsToServe)
            {
                Console.WriteLine("checking penalty validity, pen lap = " + penaltyLap + ", completed =" + lapsCompleted);
                return hasOutstandingPenalty && lapsCompleted == penaltyLap;
            }
            else
            {
                return hasOutstandingPenalty;
            }
        }
    
        override protected void triggerInternal(Shared lastState, Shared currentState) {
            if (currentState.NumPenalties > 0) 
            {
                if (currentState.NumPenalties > lastState.NumPenalties) {
                    lapsCompleted = currentState.CompletedLaps;
                    // this is a new penalty
                    audioPlayer.queueClip(folderNewPenalty, 0, this);
                    // queue a '3 laps to serve penalty' message - this might not get played
                    audioPlayer.queueClip(folderThreeLapsToServe, 20, this);
                    // we don't already have a penalty
                    if (penaltyLap == -1 || !hasOutstandingPenalty)
                    {
                        penaltyLap = currentState.CompletedLaps;
                    }
                    hasOutstandingPenalty = true;
                }
                else if (isNewLap)
                {
                    lapsCompleted = currentState.CompletedLaps;
                    if (lapsCompleted - penaltyLap == 3)
                    {
                        // what if the player is actually serving his penalty at the time?? This simply won't work reliably
                        // Also, what if the player crosses the line while serving a slow-down penalty? A short delay (5 seconds)
                        // might help a little...
                        audioPlayer.queueClip(folderDisqualified, 5, this);
                    }
                    if (lapsCompleted - penaltyLap == 2)
                    {
                        audioPlayer.queueClip(folderOneLapToServe, pitstopDelay, this);
                    }
                    else if (lapsCompleted - penaltyLap == 1)
                    {
                        audioPlayer.queueClip(folderTwoLapsToServe, pitstopDelay, this);
                    }
                }
            } else  {
                clearStateInternal();
            } 
        }
    }
}
