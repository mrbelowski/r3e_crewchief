using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChief.Data;

namespace CrewChief.Events
{
    class MandatoryPitStops : AbstractEvent
    {
        private String folderMandatoryPitStopsOptionsToPrimes = "mandatory_pit_stops/options_to_primes";

        private String folderMandatoryPitStopsPrimesToOptions = "mandatory_pit_stops/primes_to_options";

        private String folderMandatoryPitStopsPitWindowOpening = "mandatory_pit_stops/pit_window_opening";

        private String folderMandatoryPitStopsPitWindowOpen1Min = "mandatory_pit_stops/pit_window_opens_1_min";

        private String folderMandatoryPitStopsPitWindowOpen2Min = "mandatory_pit_stops/pit_window_opens_2_min";

        private String folderMandatoryPitStopsPitWindowOpen = "mandatory_pit_stops/pit_window_open";

        private String folderMandatoryPitStopsPitWindowCloses1min = "mandatory_pit_stops/pit_window_closes_1_min";

        private String folderMandatoryPitStopsPitWindowCloses2min = "mandatory_pit_stops/pit_window_closes_2_min";

        private String folderMandatoryPitStopsPitWindowClosing = "mandatory_pit_stops/pit_window_closing";

        private String folderMandatoryPitStopsPitWindowClosed = "mandatory_pit_stops/pit_window_closed";

        private String folderMandatoryPitStopsPitThisLap = "mandatory_pit_stops/pit_this_lap";

        private String folderMandatoryPitStopsPitThisLapTooLate = "mandatory_pit_stops/pit_this_lap_too_late";

        private String folderMandatoryPitStopsPitNow = "mandatory_pit_stops/pit_now";

        private int pitWindowOpenLap;

        private int pitWindowClosedLap;

        private int pitWindowOpenTime;

        private int pitWindowClosedTime;

        private Boolean pitDataInitialised;

        private Boolean onOptions;

        private Boolean onPrimes;

        private int tyreChangeLap;

        private Boolean playBoxNowMessage;

        private Boolean playOpenNow;

        private Boolean play1minOpenWarning;

        private Boolean play2minOpenWarning;

        private Boolean playClosedNow;

        private Boolean play1minCloseWarning;

        private Boolean play2minCloseWarning;

        private Boolean playPitThisLap;

        public MandatoryPitStops(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        protected override void clearStateInternal()
        {
            pitWindowOpenLap = 0;
            pitWindowClosedLap = 0;
            pitWindowOpenTime = 0;
            pitWindowClosedTime = 0;
            pitDataInitialised = false;
            onOptions = false;
            onPrimes = false;
            tyreChangeLap = 0;
            playBoxNowMessage = false;
            play2minOpenWarning = false;
            play2minCloseWarning = false;
            play1minOpenWarning = false;
            play1minCloseWarning = false;
            playClosedNow = false;
            playOpenNow = false;
            playPitThisLap = false;
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return isSessionRunning;
        }

        override protected void triggerInternal(Shared lastState, Shared currentState)
        {
            if (isRaceStarted && 
                currentState.PitWindowStatus != (int) Constant.PitWindow.Disabled && currentState.PitWindowStart != -1 && 
                currentState.SessionPhase == (int) Constant.SessionPhase.Green && currentState.SessionType == (int) Constant.Session.Race)
            {
                if (!pitDataInitialised)
                {
                    Console.WriteLine("pit start = " + currentState.PitWindowStart + ", pit end = "+ currentState.PitWindowEnd);
                    if (currentState.NumberOfLaps > 0)
                    {
                        pitWindowOpenLap = currentState.PitWindowStart;
                        pitWindowClosedLap = currentState.PitWindowEnd;
                        // DTM specific stuff...
                        if (currentState.TireType == (int)Constant.TireType.Option)
                        {
                            onOptions = true;
                            // when we've completed half distance - 1 laps, we need to come in at the end of the current lap
                            // TODO: this data might be in the block...
                            tyreChangeLap = (int)Math.Floor((double)currentState.NumberOfLaps / 2d) - 1;
                        }
                        else if (currentState.TireType == (int)Constant.TireType.Prime)
                        {
                            onPrimes = true;
                            tyreChangeLap = (int)Math.Floor((double)currentState.NumberOfLaps / 2d);
                        }
                        playPitThisLap = true;
                    }
                    else if (currentState.SessionTimeRemaining > 0)
                    {
                        pitWindowOpenTime = currentState.PitWindowStart;
                        pitWindowClosedTime = currentState.PitWindowEnd;
                        play2minOpenWarning = true;
                        play1minOpenWarning = true;
                        playOpenNow = true;
                        play2minCloseWarning = true;
                        play1minCloseWarning = true;
                        playClosedNow = true;
                        playPitThisLap = true;
                    }
                    else
                    {
                        Console.WriteLine("Error getting pit data");
                    }
                    pitDataInitialised = true;
                }
                else
                {
                    if (isNewLap && currentState.CompletedLaps > 0 && currentState.NumberOfLaps > 0)
                    {
                        if (currentState.PitWindowStatus != (int)Constant.PitWindow.StopInProgress &&
                            currentState.PitWindowStatus != (int)Constant.PitWindow.Completed && 
                            currentState.CompletedLaps == tyreChangeLap &&
                            playPitThisLap)
                        {
                            playBoxNowMessage = true;
                            playPitThisLap = false;
                            if (onOptions)
                            {
                                audioPlayer.queueClip(folderMandatoryPitStopsOptionsToPrimes, 0, this);
                            }
                            else if (onPrimes)
                            {
                                audioPlayer.queueClip(folderMandatoryPitStopsPrimesToOptions, 0, this);
                            }
                            else
                            {
                                audioPlayer.queueClip(folderMandatoryPitStopsPitThisLap, 0, this);
                            }
                        }
                        else if (currentState.CompletedLaps == pitWindowOpenLap - 1)
                        {
                            // note this is a 'pit window opens at the end of this lap' message, 
                            // so we play it 1 lap before the window opens
                            audioPlayer.queueClip(folderMandatoryPitStopsPitWindowOpening, 0, this);
                        }
                        else if (currentState.CompletedLaps == pitWindowOpenLap)
                        {
                            audioPlayer.setBackgroundSound(AudioPlayer.dtmPitWindowOpenBackground);
                            audioPlayer.queueClip(folderMandatoryPitStopsPitWindowOpen, 0, this);
                        }
                        else if (currentState.CompletedLaps == pitWindowClosedLap)
                        {
                            audioPlayer.queueClip(folderMandatoryPitStopsPitWindowClosing, 0, this);
                            if (currentState.PitWindowStatus != (int)Constant.PitWindow.Completed)
                            {
                                audioPlayer.queueClip(folderMandatoryPitStopsPitThisLap, 0, this);
                                playBoxNowMessage = true;
                            }
                        }
                        else if (currentState.CompletedLaps == pitWindowClosedLap + 1)
                        {
                            audioPlayer.setBackgroundSound(AudioPlayer.dtmPitWindowClosedBackground);
                            audioPlayer.queueClip(folderMandatoryPitStopsPitWindowClosed, 0, this);
                        }
                    }
                    else if (isNewLap && currentState.CompletedLaps > 0 && currentState.SessionTimeRemaining > 0)
                    {
                        if (currentState.PitWindowStatus != (int)Constant.PitWindow.StopInProgress &&
                            currentState.PitWindowStatus != (int)Constant.PitWindow.Completed &&
                            getTimeInRace(currentState) > pitWindowOpenTime * 60 &&
                            getTimeInRace(currentState) < pitWindowClosedTime * 60)
                        {
                            double timeLeftToPit = pitWindowClosedTime * 60 - getTimeInRace(currentState);
                            if (playPitThisLap && currentState.LapTimeBest + 10 > timeLeftToPit)
                            {
                                // oh dear, we might have missed the pit window.
                                audioPlayer.queueClip(folderMandatoryPitStopsPitThisLapTooLate, 0, this);
                                playBoxNowMessage = true;
                                playPitThisLap = false;
                            }
                            else if (currentState.LapTimeBest + 10 < timeLeftToPit && (currentState.LapTimeBest * 2) + 10 > timeLeftToPit)
                            {
                                // we probably won't make it round twice - pit at the end of this lap
                                audioPlayer.queueClip(folderMandatoryPitStopsPitThisLap, 0, this);
                                playBoxNowMessage = true;
                                playPitThisLap = false;
                            }
                        }
                    }
                    if (playOpenNow && currentState.SessionTimeRemaining > 0 &&
                        (getTimeInRace(currentState) > (pitWindowOpenTime * 60) || currentState.PitWindowStatus == (int)Constant.PitWindow.Open))
                    {
                        playOpenNow = false;
                        play1minOpenWarning = false;
                        play2minOpenWarning = false;
                        audioPlayer.queueClip(folderMandatoryPitStopsPitWindowOpen, 0, this);
                    }
                    else if (play1minOpenWarning && currentState.SessionTimeRemaining > 0 && getTimeInRace(currentState) > ((pitWindowOpenTime - 1) * 60))
                    {
                        play1minOpenWarning = false;
                        play2minOpenWarning = false;
                        audioPlayer.queueClip(folderMandatoryPitStopsPitWindowOpen1Min, 0, this);
                    }
                    else if (play2minOpenWarning && currentState.SessionTimeRemaining > 0 && getTimeInRace(currentState) > ((pitWindowOpenTime - 2) * 60))
                    {
                        play2minOpenWarning = false;
                        audioPlayer.queueClip(folderMandatoryPitStopsPitWindowOpen2Min, 0, this);
                    }
                    else if (playClosedNow && currentState.SessionTimeRemaining > 0 &&
                    (getTimeInRace(currentState) > (pitWindowClosedTime * 60)))
                    {
                        playClosedNow = false;
                        playBoxNowMessage = false;
                        play1minCloseWarning = false;
                        play2minCloseWarning = false;
                        playPitThisLap = false;
                        audioPlayer.queueClip(folderMandatoryPitStopsPitWindowClosed, 0, this);
                    }
                    else if (play1minCloseWarning && currentState.SessionTimeRemaining > 0 && getTimeInRace(currentState) > ((pitWindowClosedTime - 1) * 60))
                    {
                        play1minCloseWarning = false;
                        play2minCloseWarning = false;
                        audioPlayer.queueClip(folderMandatoryPitStopsPitWindowCloses1min, 0, this);
                    }
                    else if (play2minCloseWarning && currentState.SessionTimeRemaining > 0 && getTimeInRace(currentState) > ((pitWindowClosedTime - 2) * 60))
                    {
                        play2minCloseWarning = false;
                        audioPlayer.queueClip(folderMandatoryPitStopsPitWindowCloses2min, 0, this);
                    }
                                        
                    if (playBoxNowMessage && currentLapSector == 3)
                    {
                        audioPlayer.queueClip(folderMandatoryPitStopsPitNow, 0, this);
                        playBoxNowMessage = false;
                    }
                }
            }
        }
    }
}
