using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChief.Data;

namespace CrewChief.Events
{
    // note this only works for DTM as non-DTM events don't have number of laps *or* a race time remaining
    // field in the data block
    class LapCounter : AbstractEvent
    {
        private String folderGreenGreenGreen = "lap_counter/green_green_green";

        private String folderGetReady = "lap_counter/get_ready";

        private String folderLastLap = "lap_counter/last_lap";
    
        private String folderTwoLeft = "lap_counter/two_to_go";
    
        private String folderLastLapLeading = "lap_counter/last_lap_leading";
    
        private String folderLastLapTopThree = "lap_counter/last_lap_top_three";
    
        private String folderTwoLeftLeading = "lap_counter/two_to_go_leading";

        private String folderTwoLeftTopThree = "lap_counter/two_to_go_top_three";

        private String folderPodiumFinish = "lap_counter/podium_finish";

        private String folderWonRace = "lap_counter/won_race";

        private String folderFinishedRace = "lap_counter/finished_race";

        private String folderFinishedRaceLast = "lap_counter/finished_race_last";

        Boolean playedGreenGreenGreen;
        Boolean playedGetReady;

        Boolean playedFinished;
    
        public LapCounter(AudioPlayer audioPlayer) {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            playedGreenGreenGreen = false;
            playedGetReady = false;
            playedFinished = false;
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return true;
        }
        
        override protected void triggerInternal(Shared lastState, Shared currentState)
        {            
            if (!playedGetReady && 
                (currentState.SessionPhase == (int) Constant.SessionPhase.Countdown))
            {
                audioPlayer.openChannel();
                audioPlayer.playClipImmediately(folderGetReady, new QueuedMessage(0, this));
                playedGetReady = true;
                audioPlayer.closeChannel();
            }
            if (!playedGreenGreenGreen && 
                (lastState.SessionPhase == (int) Constant.SessionPhase.Countdown && currentState.SessionPhase == (int) Constant.SessionPhase.Green) ||
                (lastState.ControlType ==(int) Constant.Control.AI && currentState.ControlType == (int) Constant.Control.Player && 
                currentState.Player.GameSimulationTime < 20))
            {
                audioPlayer.openChannel();
                audioPlayer.playClipImmediately(folderGreenGreenGreen, new QueuedMessage(0, this));
                audioPlayer.closeChannel();
                playedGreenGreenGreen = true;
            }
            if (!playedFinished && CommonData.isNewLap && currentState.Player.GameSimulationTime > 60 && 
                currentState.SessionPhase == (int)Constant.SessionPhase.Checkered) 
            {
                int position = currentState.Position;
                if (lastState.Position !=  0 && lastState.Position != position)
                {
                    Console.WriteLine("At end of race, position has changed...");
                    if (position + 1 > lastState.Position)
                    {
                        Console.WriteLine("According to memory block, the player has lost at least 2 positions as he crossed the line, using last state position");
                        position = lastState.Position;
                    }
                    else if (position - 1 < lastState.Position)
                    {
                        Console.WriteLine("According to memory block, the player has gained at least 2 positions as he crossed the line, using last state position");
                        position = lastState.Position;
                    }
                }
                if (position == 1)
                {
                    audioPlayer.queueClip(folderWonRace, 0, this);
                }
                else if (position < 4)
                {
                    audioPlayer.queueClip(folderPodiumFinish, 0, this);
                }
                else if (position >= 4 && !CommonData.isLast)
                {
                    audioPlayer.queueClip(folderFinishedRace, 0, this);
                }
                else if (CommonData.isLast)
                {
                    audioPlayer.queueClip(folderFinishedRaceLast, 0, this);
                }
                else
                {
                    Console.WriteLine("Race finished but position is 0");
                }
                playedFinished = true;
            }
            if (CommonData.isRaceStarted && CommonData.isNewLap && currentState.NumberOfLaps > 0) {
                // a new lap has been started in race mode
                Console.WriteLine("LapCounter event: position at lap " + currentState.CompletedLaps + " = " + currentState.Position);
                int position = currentState.Position;
                if (position < 1)
                {
                    Console.WriteLine("Position in current data block = " + position + " using position in previous data block "+ lastState.Position);
                    position = lastState.Position;
                }
                if (currentState.CompletedLaps == currentState.NumberOfLaps - 1) {
                    if (currentState.Position == 1) {
                        audioPlayer.queueClip(folderLastLapLeading, 0, this);
                    } else if (currentState.Position < 4) {
                        audioPlayer.queueClip(folderLastLapTopThree, 0, this);
                    }
                    else if (currentState.Position >= 4)
                    {
                        audioPlayer.queueClip(folderLastLap, 0, this, PearlsOfWisdom.PearlType.NEUTRAL, 0.5);
                    }
                    else if (currentState.Position >= 10)
                    {
                        audioPlayer.queueClip(folderLastLap, 0, this, PearlsOfWisdom.PearlType.BAD, 0.5);
                    }
                    else
                    {
                        Console.WriteLine("1 lap left but position is 0");
                    }
                }
                else if (currentState.CompletedLaps == currentState.NumberOfLaps - 2)
                {
                    if (currentState.Position == 1) {
                        audioPlayer.queueClip(folderTwoLeftLeading, 0, this);
                    } else if (currentState.Position < 4) {
                        audioPlayer.queueClip(folderTwoLeftTopThree, 0, this);
                    }
                    else if (currentState.Position >= 4)
                    {
                        audioPlayer.queueClip(folderTwoLeft, 0, this, PearlsOfWisdom.PearlType.NEUTRAL, 0.5);
                    }
                    else if (currentState.Position >= 10)
                    {
                        audioPlayer.queueClip(folderTwoLeft, 0, this, PearlsOfWisdom.PearlType.BAD, 0.5);
                    }
                    else
                    {
                        Console.WriteLine("2 laps left but position is 0");
                    }
                } 
            }
        }  
    }
}
