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

        // if the closing speed is > 5ms (about 12mph) then don't trigger spotter messages - 
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
            float deltaFront = Math.Abs(currentState.TimeDeltaFront);          
            float deltaBehind = Math.Abs(currentState.TimeDeltaBehind);
            
            if (isRaceStarted && currentState.Player.GameSimulationTime > timeAfterRaceStartToActivate && 
                currentState.ControlType == (int)Constant.Control.Player && speed > minSpeedForSpotterToOperate)
            {
                if (deltaFront < 0 && deltaBehind < 0)
                {
                    Console.WriteLine("Both deltas are < 0, " + deltaFront + ", " + deltaBehind);
                    return;
                }

                // if we think there's already a car along side, add a little to the car length so we're
                // sure it's gone before calling clear
                float carLengthToUse = carLength;
                if (channelOpen)
                {
                    carLengthToUse += gapNeededForClear;
                }

                Boolean carAlongSideInFront = carLengthToUse / speed > deltaFront;
                Boolean carAlongSideBehind = carLengthToUse / speed > deltaBehind;
                DateTime now = DateTime.Now;

                if (channelOpen && !carAlongSideInFront && !carAlongSideBehind) 
                {
                    Console.WriteLine("think we're clear, deltaFront = " + deltaFront + " time gap = " + carLengthToUse / speed);
                    Console.WriteLine("deltaBehind = " + deltaBehind + " time gap = " + carLengthToUse / speed);
                    Console.WriteLine("race time = " + currentState.Player.GameSimulationTime);

                    if (now > timeWhenWeThinkWeAreClear.Add(clearMessageDelay)) {
                        channelOpen = false;
                        audioPlayer.removeImmediateClip(folderHoldYourLine);
                        audioPlayer.removeImmediateClip(folderStillThere);
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
                    if ((carAlongSideInFront && Math.Abs(closingSpeedInFront) < maxClosingSpeed && 
                        (closingSpeedInFront > 0 || channelOpen)) ||
                        (carAlongSideBehind && Math.Abs(closingSpeedBehind) < maxClosingSpeed &&
                        (closingSpeedBehind > 0 || channelOpen)))
                    {
                        Console.WriteLine("think we're overlapping, deltaFront = " + deltaFront + " time gap = " +
                            carLengthToUse / speed + " closing speed = " + closingSpeedInFront);
                        Console.WriteLine("deltaBehind = " + deltaBehind + " time gap = " +
                            carLengthToUse / speed + " closing speed = " + closingSpeedBehind);
                        Console.WriteLine("race time = " + currentState.Player.GameSimulationTime);

                        if (!channelOpen)
                        {
                            timeOfLastHoldMessage = now;
                            channelOpen = true;
                            audioPlayer.removeImmediateClip(folderClear);
                            audioPlayer.removeImmediateClip(folderStillThere);
                            audioPlayer.openChannel();
                            QueuedMessage holdMessage = new QueuedMessage(0, this);
                            holdMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + holdMessageExpiresAfter;
                        
                            audioPlayer.playClipImmediately(folderHoldYourLine, holdMessage);
                        } else if (now > timeOfLastHoldMessage.Add(repeatHoldFrequency)) {
                            timeOfLastHoldMessage = now;
                            audioPlayer.removeImmediateClip(folderHoldYourLine);
                            audioPlayer.removeImmediateClip(folderClear);
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
            float averageSpeed = (currentState.CarSpeed + lastState.CarSpeed) / 2;
            float timeElapsed = (float)currentState.Player.GameSimulationTime - (float)lastState.Player.GameSimulationTime;
            if (front)
            {
                return ((Math.Abs(lastState.TimeDeltaFront) / averageSpeed) - 
                    (Math.Abs(currentState.TimeDeltaFront) / averageSpeed)) / timeElapsed;
            } else
            {
                return ((Math.Abs(lastState.TimeDeltaBehind) / averageSpeed) -
                    (Math.Abs(currentState.TimeDeltaBehind) / averageSpeed)) / timeElapsed;
            }
        }
    }
}
