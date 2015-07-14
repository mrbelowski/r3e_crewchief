using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChief.Data;
using System.Threading;

namespace CrewChief.Events
{
    class Spotter : AbstractEvent
    {
        // if the audio player is in the middle of another message, this 'immediate' message will have to wait.
        // If it's older than 1000 milliseconds by the time the player's got round to playing it, it's expired
        private int clearMessageExpiresAfter = 2000;               
        private int holdMessageExpiresAfter = 1000;

        // how long is a car? we use 3.5 meters by default here. Too long and we'll get 'hold your line' messages
        // when we're clearly directly behind the car
        private float carLength = Properties.Settings.Default.spotter_car_length;

        // before saying 'clear', we need to be carLength + this value from the other car
        private float gapNeededForClear = Properties.Settings.Default.spotter_gap_for_clear;

        // don't play spotter messages if we're going < 10ms
        private float minSpeedForSpotterToOperate = Properties.Settings.Default.min_speed_for_spotter;

        // if the closing speed is > 10ms (about 17mph) then don't trigger spotter messages - 
        // this prevents them being triggered when passing stationary cars
        private float maxClosingSpeed = Properties.Settings.Default.max_closing_speed_for_spotter;

        // don't activate the spotter unless this many seconds have elapsed (race starts are messy)
        private int timeAfterRaceStartToActivate = Properties.Settings.Default.time_after_race_start_for_spotter;

        // say "still there" every 3 seconds
        private TimeSpan repeatHoldFrequency = TimeSpan.FromSeconds(Properties.Settings.Default.spotter_hold_repeat_frequency);

        private Boolean channelOpen;

        private String folderClear = "spotter/clear";
        private String folderHoldYourLine = "spotter/hold_your_line";
        private String folderStillThere = "spotter/still_there";

        // don't play 'clear' messages unless we've actually been clear for 0.5 seconds
        private TimeSpan clearMessageDelay = TimeSpan.FromMilliseconds(500);

        private DateTime timeOfLastHoldMessage;

        private DateTime timeWhenWeThinkWeAreClear;

        public Spotter(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        protected override void clearStateInternal()
        {
            channelOpen = false;
            timeOfLastHoldMessage = DateTime.Now;
            timeWhenWeThinkWeAreClear = DateTime.Now;
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return isSessionRunning;
        }

        override protected void triggerInternal(Shared lastState, Shared currentState)
        {
            float speed = currentState.CarSpeed;
            float deltaFront = currentState.TimeDeltaFront;          
            float deltaBehind = currentState.TimeDeltaBehind;
            if (deltaFront < 0 && deltaBehind < 0)
            {
                Console.WriteLine("Both deltas are < 0, " + deltaFront + ", " + deltaBehind);
                return;
            }

            if (isRaceStarted && currentState.Player.GameSimulationTime > timeAfterRaceStartToActivate && speed > minSpeedForSpotterToOperate)
            {
                // if we think there's already a car along side, add a little to the car length so we're
                // sure it's gone before calling clear
                float carLengthToUse = carLength;
                if (channelOpen)
                {
                    carLengthToUse += gapNeededForClear;
                }

                Boolean carAlongSideInFront = deltaFront > -1 && carLengthToUse / speed > deltaFront;
                Boolean carAlongSideBehind = deltaBehind > -1 && carLengthToUse / speed > deltaBehind;
                DateTime now = DateTime.Now;

                if (channelOpen && !carAlongSideInFront && !carAlongSideBehind) 
                {
                    Console.WriteLine("think we're clear, deltaFront = " + deltaFront + " time gap = " + carLengthToUse / speed);
                    Console.WriteLine("think we're clear, deltaBehind = " + deltaFront + " time gap = " + carLengthToUse / speed);

                    if (now > timeWhenWeThinkWeAreClear.Add(clearMessageDelay)) {
                        channelOpen = false;
                        QueuedMessage clearMessage = new QueuedMessage(0, this);
                        clearMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + clearMessageExpiresAfter;
                        audioPlayer.playClipImmediately(folderClear, clearMessage);
                        audioPlayer.closeChannel();
                    } else {
                        timeWhenWeThinkWeAreClear = now;
                    }
                }
                else if (carAlongSideInFront || carAlongSideBehind)
                {                    
                    // check the closing speed before warning
                    float closingSpeedInFront = getClosingSpeed(lastState, currentState, true);
                    float closingSpeedBehind = getClosingSpeed(lastState, currentState, false);
                    if ((carAlongSideInFront && closingSpeedInFront > -1 && closingSpeedInFront < maxClosingSpeed) ||
                        (carAlongSideBehind && closingSpeedBehind > -1 && closingSpeedBehind < maxClosingSpeed))
                    {
                        if (!channelOpen)
                        {
                            timeOfLastHoldMessage = now;
                            channelOpen = true;
                            audioPlayer.openChannel();
                            QueuedMessage holdMessage = new QueuedMessage(0, this);
                            holdMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + holdMessageExpiresAfter;
                        
                            audioPlayer.playClipImmediately(folderHoldYourLine, holdMessage);
                        } else if (now > timeOfLastHoldMessage.Add(repeatHoldFrequency)) {
                            timeOfLastHoldMessage = now;
                            QueuedMessage stillThereMessage = new QueuedMessage(0, this);
                            stillThereMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + holdMessageExpiresAfter;

                            audioPlayer.playClipImmediately(folderStillThere, stillThereMessage);
                        }            
                    }                    
                }
            }
            else if (channelOpen)
            {
                channelOpen = false;
                audioPlayer.closeChannel();
            }
        }

        private float getClosingSpeed(Shared lastState, Shared currentState, Boolean front)
        {
            float timeElapsed = (float)currentState.Player.GameSimulationTime - (float)lastState.Player.GameSimulationTime;
            if (front)
            {
                if (timeElapsed <= 0 || lastState.TimeDeltaFront == -1 || currentState.TimeDeltaFront == -1)
                {
                    return -1;
                }
                return ((currentState.TimeDeltaFront / currentState.CarSpeed) - (lastState.TimeDeltaFront / lastState.CarSpeed)) / timeElapsed;
            } else
            {
                if (timeElapsed <= 0 || lastState.TimeDeltaBehind == -1 || currentState.TimeDeltaBehind == -1)
                {
                    return -1;
                }
                return ((currentState.TimeDeltaBehind / currentState.CarSpeed) - (lastState.TimeDeltaBehind / lastState.CarSpeed)) / timeElapsed;
            }
        }
    }
}
