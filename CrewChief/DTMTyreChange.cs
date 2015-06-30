using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChief.Data;

namespace CrewChief.Events
{
    class DTMTyreChange : AbstractEvent
    {
        private String folderDTMOptionsToPrimes = "dtm_tyre_change/options_to_primes";

        private String folderDTMPrimesToOptions = "dtm_tyre_change/primes_to_options";

        private String folderDTMPitWindowOpening = "dtm_tyre_change/pit_window_opening";

        private String folderDTMPitWindowOpen = "dtm_tyre_change/pit_window_open";

        private String folderDTMPitWindowClosing = "dtm_tyre_change/pit_window_closing";

        private String folderDTMPitWindowClosed = "dtm_tyre_change/pit_window_closed";

        private int optionsToPrimeLap;

        private int primesToOptionsLap;

        private int pitWindowOpenLap;

        private int pitWindowClosingLap;

        private Boolean isDTM;

        private Boolean pitDataInitialised;

        public DTMTyreChange(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        protected override void clearStateInternal()
        {
            optionsToPrimeLap = 0;
            primesToOptionsLap = 0;
            pitWindowOpenLap = 0;
            pitWindowClosingLap = 0;
            isDTM = false;
            pitDataInitialised = false;
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return true;
        }

        override protected void triggerInternal(Shared lastState, Shared currentState)
        {
            if (isRaceMode)
            {
                if (!pitDataInitialised)
                {
                    // grotty hack. There's no data in the gamestate indicating which car type is being used. Only DTM has
                    // numberOfLaps so just use that. Yuk

                    // TODO: this code assumes that anyone playing DTM 2014 will have mandatory tyre changes enabled. 
                    // There's no way of knowing whether this is the case from the current memory block contents so we 
                    // have to assume it is enabled...
                    if (currentState.NumberOfLaps > 0)
                    {

                        // laps 7, options to primes 2, primes to options 3, opens 2 closes 5

                        // TODO: verify these calculations. They appear to break down when the race length
                        // is very short
                        isDTM = true;
                        int nlaps = currentState.NumberOfLaps;
                        optionsToPrimeLap = (int) Math.Round((float) nlaps / 2.0f) - 1;
                        primesToOptionsLap = optionsToPrimeLap + 1;
                        pitWindowOpenLap = (int) Math.Round((float) nlaps / 3.0f);
                        pitWindowClosingLap = pitWindowOpenLap + 3;
                        Console.WriteLine("Setting up pit window data. Num laps = " + nlaps + ", options to prime lap = " + optionsToPrimeLap +
                            ", primes to options lap = " + primesToOptionsLap + ", window opens at end of lap " + pitWindowOpenLap + ", window closes at end of lap " + pitWindowClosingLap);
                    }
                    else
                    {
                        Console.WriteLine("NumberOfLaps < 0 so this doesn't appear to be a DTM race session");
                        isDTM = false;
                    }
                    pitDataInitialised = true;
                }
                else
                {
                    if (isNewLap && isDTM && currentState.CompletedLaps > 0)
                    {
                        if (currentState.CompletedLaps == optionsToPrimeLap)
                        {
                            audioPlayer.queueClip(folderDTMOptionsToPrimes, 0, this);
                        }
                        else if (currentState.CompletedLaps == primesToOptionsLap)
                        {
                            audioPlayer.queueClip(folderDTMPrimesToOptions, 0, this);
                        }
                        else if (currentState.CompletedLaps == pitWindowOpenLap - 1)
                        {
                            // note this is a 'pit window opens at the end of this lap' message, 
                            // so we play it 1 lap before the window opens
                            audioPlayer.queueClip(folderDTMPitWindowOpening, 0, this);
                        }
                        else if (currentState.CompletedLaps == pitWindowOpenLap)
                        {
                            audioPlayer.setBackgroundSound(AudioPlayer.dtmPitWindowOpenBackground);
                            audioPlayer.queueClip(folderDTMPitWindowOpen, 0, this);
                        }
                        else if (currentState.CompletedLaps == pitWindowClosingLap)
                        {
                            audioPlayer.queueClip(folderDTMPitWindowClosing, 0, this);
                        }
                        else if (currentState.CompletedLaps == pitWindowClosingLap + 1)
                        {
                            audioPlayer.setBackgroundSound(AudioPlayer.dtmPitWindowClosedBackground);
                            audioPlayer.queueClip(folderDTMPitWindowClosed, 0, this);
                        }
                    }
                }
            }
        }
    }
}
