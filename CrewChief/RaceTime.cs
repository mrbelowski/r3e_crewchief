using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChief.Events;
using CrewChief.Data;

namespace CrewChief.Events
{
    // interim event to read out the time elapsed. When the time remaining is in the data block
    // this class can be replaced
    class RaceTime : AbstractEvent
    {
        private String folder5mins = "race_time/five_minutes_left";
        private String folder5minsLeading = "race_time/five_minutes_left_leading";
        private String folder5minsPodium = "race_time/five_minutes_left_podium";
        private String folder10mins = "race_time/ten_minutes_left";
        private String folder15mins = "race_time/fifteen_minutes_left";
        private String folder20mins = "race_time/twenty_minutes_left";
        private String folderHalfWayHome = "race_time/half_way";
        private String folderLastLap = "race_time/last_lap";
        private String folderLastLapLeading = "race_time/last_lap_leading";
        private String folderLastLapPodium = "race_time/last_lap_top_three";

        private Boolean played5mins, played10mins, played15mins, played20mins, playedHalfWayHome, playedLastLap;

        private float halfTime;

        private Boolean gotHalfTime;

        public RaceTime(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        protected override void clearStateInternal()
        {
            played5mins = false; played10mins = false; played15mins = false;
            played20mins = false; playedHalfWayHome = false; playedLastLap = false ;
            halfTime = 0;
            gotHalfTime = false;
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return isSessionRunning;
        }

        override protected void triggerInternal(Shared lastState, Shared currentState)
        {
            if (currentState.SessionTimeRemaining > -1 && isSessionRunning)
            {
                if (!gotHalfTime)
                {
                    Console.WriteLine("Session time remaining = " + currentState.SessionTimeRemaining);
                    halfTime = currentState.SessionTimeRemaining / 2;
                    gotHalfTime = true;
                    if (isRaceStarted && currentState.FuelUseActive == 1)
                    {
                        // don't allow the half way message to play if fuel use is active - there's already one in there
                        playedHalfWayHome = true;
                    }
                }
                PearlsOfWisdom.PearlType pearlType = PearlsOfWisdom.PearlType.NONE;
                if (currentState.CompletedLaps > 1)
                {
                    pearlType = PearlsOfWisdom.PearlType.NEUTRAL;
                    if (currentState.Position < 4)
                    {
                        pearlType = PearlsOfWisdom.PearlType.GOOD;
                    }
                    else if (currentState.Position > 10)
                    {
                        pearlType = PearlsOfWisdom.PearlType.BAD;
                    }                
                }

                if (isRaceStarted && currentState.Player.GameSimulationTime > 60 && !playedLastLap &&
                    currentState.SessionTimeRemaining < currentState.LapTimeBest - 5)
                {
                    playedLastLap = true;
                    played5mins = true;
                    played10mins = true;
                    played15mins = true;
                    played20mins = true;
                    playedHalfWayHome = true;
                    if (currentState.Position == 1)
                    {
                        // don't add a pearl here - the audio clip already contains encouragement
                        audioPlayer.queueClip(folderLastLapLeading, 0, this, pearlType, 0);
                    }
                    else if (currentState.Position < 4)
                    {
                        // don't add a pearl here - the audio clip already contains encouragement
                        audioPlayer.queueClip(folderLastLapPodium, 0, this, pearlType, 0);
                    }
                    else
                    {
                        audioPlayer.queueClip(folderLastLap, 0, this, pearlType, 0.7);
                    }
                } if (currentState.Player.GameSimulationTime > 60 && !played5mins &&
                    currentState.SessionTimeRemaining / 60 < 5)
                {
                    played5mins = true;
                    played10mins = true;
                    played15mins = true;
                    played20mins = true;
                    playedHalfWayHome = true;
                    if (isRaceStarted && currentState.Position == 1)
                    {
                        // don't add a pearl here - the audio clip already contains encouragement
                        audioPlayer.queueClip(folder5minsLeading, 0, this, pearlType, 0);
                    }
                    else if (isRaceStarted && currentState.Position < 4)
                    {
                        // don't add a pearl here - the audio clip already contains encouragement
                        audioPlayer.queueClip(folder5minsPodium, 0, this, pearlType, 0);
                    }
                    else
                    {
                        audioPlayer.queueClip(folder5mins, 0, this, pearlType, 0.7);
                    }
                }
                if (currentState.Player.GameSimulationTime > 60 && !played10mins && currentState.SessionTimeRemaining / 60 < 10 && currentState.SessionTimeRemaining / 60 > 9.9)
                {
                    played10mins = true;
                    played15mins = true;
                    played20mins = true;
                    audioPlayer.queueClip(folder10mins, 0, this, pearlType, 0.7);
                }
                if (currentState.Player.GameSimulationTime > 60 && !played15mins && currentState.SessionTimeRemaining / 60 < 15 && currentState.SessionTimeRemaining / 60 > 14.9)
                {
                    played15mins = true;
                    played20mins = true;
                    audioPlayer.queueClip(folder15mins, 0, this, pearlType, 0.7);
                }
                if (currentState.Player.GameSimulationTime > 60 && !played20mins && currentState.SessionTimeRemaining / 60 < 20 && currentState.SessionTimeRemaining / 60 > 19.9)
                {
                    played20mins = true;
                    audioPlayer.queueClip(folder20mins, 0, this, pearlType, 0.7);
                }
                else if (currentState.Player.GameSimulationTime > 60 && !playedHalfWayHome && currentState.SessionTimeRemaining < halfTime)
                {
                    playedHalfWayHome = true;
                    audioPlayer.queueClip(folderHalfWayHome, 0, this, pearlType, 0.7);
                }
            }
        }
    }
}
