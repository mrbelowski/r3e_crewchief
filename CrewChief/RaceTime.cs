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
        private String folder5mins = "race_time/five_minutes";
        private String folder10mins = "race_time/ten_minutes";
        private String folder15mins = "race_time/fifteen_minutes";
        private String folder20mins = "race_time/twenty_minutes";
        private String folder25mins = "race_time/twenty_five_minutes";
        private String folder30mins = "race_time/thirty_minutes";
        private String folder35mins = "race_time/thirty_five_minutes";
        private String folder40mins = "race_time/forty_minutes";
        private String folder45mins = "race_time/forty_five_minutes";
        private String folder50mins = "race_time/fifty_minutes";
        private String folder55mins = "race_time/fifty_five_minutes";
        private String folder60mins = "race_time/sixty_minutes";

        private Boolean played5mins, played10mins, played15mins, played20mins, played25mins,
            played30mins, played35mins, played40mins, played45mins, played50mins, played55mins, played60mins;

        public RaceTime(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        protected override void clearStateInternal()
        {
            played5mins = false; played10mins = false; played15mins = false; played20mins = false; played25mins = false;
            played30mins = false; played35mins = false; played40mins = false; played45mins = false; played50mins = false;
            played55mins = false; played60mins = false;
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return true;
        }

        override protected void triggerInternal(Shared lastState, Shared currentState)
        {
            // only do this if the session doesn't have a fixed number of laps
            if (currentState.NumberOfLaps < 1)
            {
                double gameTimeMinutes = currentState.Player.GameSimulationTime / 60;
                PearlsOfWisdom.PearlType pearlType = PearlsOfWisdom.PearlType.NEUTRAL;
                if (currentState.Position < 4)
                {
                    pearlType = PearlsOfWisdom.PearlType.GOOD;
                }
                else if (currentState.Position > 10)
                {
                    pearlType = PearlsOfWisdom.PearlType.BAD;
                }

                if (!played5mins && gameTimeMinutes > 5 && gameTimeMinutes <= 10)
                {
                    played5mins = true;
                    audioPlayer.queueClip(folder5mins, 0, this, pearlType, 0.7);
                }
                else if (!played10mins && gameTimeMinutes > 10 && gameTimeMinutes <= 15)
                {
                    played10mins = true;
                    played5mins = true;
                    audioPlayer.queueClip(folder10mins, 0, this, pearlType, 0.7);
                }
                else if (!played15mins && gameTimeMinutes > 15 && gameTimeMinutes <= 20)
                {
                    played15mins = true;
                    played10mins = true;
                    played5mins = true;
                    audioPlayer.queueClip(folder15mins, 0, this, pearlType, 0.7);
                }
                else if (!played20mins && gameTimeMinutes > 20 && gameTimeMinutes <= 25)
                {
                    played20mins = true;
                    played15mins = true;
                    played10mins = true;
                    played5mins = true;
                    audioPlayer.queueClip(folder20mins, 0, this, pearlType, 0.7);
                }
                else if (!played25mins && gameTimeMinutes > 25 && gameTimeMinutes <= 30)
                {
                    played25mins = true;
                    played20mins = true;
                    played15mins = true;
                    played10mins = true;
                    played5mins = true;
                    audioPlayer.queueClip(folder25mins, 0, this, pearlType, 0.7);
                }
                else if (!played30mins && gameTimeMinutes > 30 && gameTimeMinutes <= 35)
                {
                    played30mins = true;
                    played25mins = true;
                    played20mins = true;
                    played15mins = true;
                    played10mins = true;
                    played5mins = true;
                    audioPlayer.queueClip(folder30mins, 0, this, pearlType, 0.7);
                }
                else if (!played35mins && gameTimeMinutes > 35 && gameTimeMinutes <= 40)
                {
                    played35mins = true;
                    played30mins = true;
                    played25mins = true;
                    played20mins = true;
                    played15mins = true;
                    played10mins = true;
                    played5mins = true;
                    audioPlayer.queueClip(folder35mins, 0, this, pearlType, 0.7);
                }
                else if (!played40mins && gameTimeMinutes > 40 && gameTimeMinutes <= 45)
                {
                    played40mins = true;
                    played35mins = true;
                    played30mins = true;
                    played25mins = true;
                    played20mins = true;
                    played15mins = true;
                    played10mins = true;
                    played5mins = true;
                    audioPlayer.queueClip(folder40mins, 0, this, pearlType, 0.7);
                }
                else if (!played45mins && gameTimeMinutes > 45 && gameTimeMinutes <= 50)
                {
                    played45mins = true;
                    played40mins = true;
                    played35mins = true;
                    played30mins = true;
                    played25mins = true;
                    played20mins = true;
                    played15mins = true;
                    played10mins = true;
                    played5mins = true;
                    audioPlayer.queueClip(folder45mins, 0, this, pearlType, 0.7);
                }
                else if (!played50mins && gameTimeMinutes > 50 && gameTimeMinutes <= 55)
                {
                    played50mins = true;
                    played45mins = true;
                    played40mins = true;
                    played35mins = true;
                    played30mins = true;
                    played25mins = true;
                    played20mins = true;
                    played15mins = true;
                    played10mins = true;
                    played5mins = true;
                    audioPlayer.queueClip(folder50mins, 0, this, pearlType, 0.7);
                }
                else if (!played55mins && gameTimeMinutes > 55 && gameTimeMinutes <= 60)
                {
                    played55mins = true;
                    played50mins = true;
                    played45mins = true;
                    played40mins = true;
                    played35mins = true;
                    played30mins = true;
                    played25mins = true;
                    played20mins = true;
                    played15mins = true;
                    played10mins = true;
                    played5mins = true;
                    audioPlayer.queueClip(folder55mins, 0, this, pearlType, 0.5);
                }
                else if (!played60mins && gameTimeMinutes > 60)
                {
                    played60mins = true;
                    played55mins = true;
                    played50mins = true;
                    played45mins = true;
                    played40mins = true;
                    played35mins = true;
                    played30mins = true;
                    played25mins = true;
                    played20mins = true;
                    played15mins = true;
                    played10mins = true;
                    played5mins = true;
                    audioPlayer.queueClip(folder60mins, 0, this, pearlType, 0.5);
                }
            }
        }
    }
}
