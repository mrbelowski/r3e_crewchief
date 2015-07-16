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

        private Boolean spotterOnlyWhenBeingPassed = Properties.Settings.Default.spotter_only_when_being_passed;

        private Boolean enableInQualAndPractice = Properties.Settings.Default.spotter_in_qual_and_practice;

        private Boolean channelOpen;

        private String folderClear = "spotter/clear";
        private String folderHoldYourLine = "spotter/hold_your_line";
        private String folderStillThere = "spotter/still_there";

        // don't play 'clear' messages unless we've actually been clear for 0.5 seconds
        private TimeSpan clearMessageDelay = TimeSpan.FromMilliseconds(500);
        private TimeSpan overlapMessageDelay = TimeSpan.FromMilliseconds(100);

        private DateTime timeOfLastHoldMessage;

        private DateTime timeWhenWeThinkWeAreClear;
        private DateTime timeWhenWeThinkWeAreOverlapping;

        public Spotter(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        protected override void clearStateInternal()
        {
            channelOpen = false;
            timeOfLastHoldMessage = DateTime.Now;
            timeWhenWeThinkWeAreClear = DateTime.Now;
            timeWhenWeThinkWeAreOverlapping = DateTime.Now;
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return isSessionRunning;
        }

        override protected void triggerInternal(Shared lastState, Shared currentState)
        {
            float currentSpeed = currentState.CarSpeed;
            float previousSpeed = lastState.CarSpeed;
            if ((isRaceStarted || (enableInQualAndPractice && isSessionRunning)) && 
                currentState.Player.GameSimulationTime > timeAfterRaceStartToActivate &&
                currentState.ControlType == (int)Constant.Control.Player && currentSpeed > minSpeedForSpotterToOperate)
            {
                float deltaFront = Math.Abs(currentState.TimeDeltaFront);
                float deltaBehind = Math.Abs(currentState.TimeDeltaBehind);
                
                // if we think there's already a car along side, add a little to the car length so we're
                // sure it's gone before calling clear
                float carLengthToUse = carLength;
                if (channelOpen)
                {
                    carLengthToUse += gapNeededForClear;
                }

                // initialise to some large value and put the real value in here only if the
                // time gap suggests we're overlapping
                float closingSpeedInFront = 9999;
                float closingSpeedBehind = 9999;

                Boolean carAlongSideInFront = carLengthToUse / currentSpeed > deltaFront;
                Boolean carAlongSideInFrontPrevious = carLengthToUse / previousSpeed > Math.Abs(lastState.TimeDeltaFront);
                Boolean carAlongSideBehind = carLengthToUse / currentSpeed > deltaBehind;
                Boolean carAlongSideBehindPrevious = carLengthToUse / previousSpeed > Math.Abs(lastState.TimeDeltaBehind);

                // only say a car is overlapping if it's been overlapping for 2 game state updates
                // and the closing speed isn't too high
                if (carAlongSideInFront)
                {
                    // check the closing speed before warning
                    closingSpeedInFront = getClosingSpeed(lastState, currentState, true);
                }
                if (carAlongSideBehind)
                {
                    // check the closing speed before warning
                    closingSpeedBehind = getClosingSpeed(lastState, currentState, false);
                }

                DateTime now = DateTime.Now;

                if (channelOpen && !carAlongSideInFront && !carAlongSideInFrontPrevious && 
                    !carAlongSideBehindPrevious && !carAlongSideBehind) 
                {
                    Console.WriteLine("think we're clear, deltaFront = " + deltaFront + " time gap = " + carLengthToUse / currentSpeed);
                    Console.WriteLine("deltaBehind = " + deltaBehind + " time gap = " + carLengthToUse / currentSpeed);
                    Console.WriteLine("race time = " + currentState.Player.GameSimulationTime);

                    if (now > timeWhenWeThinkWeAreClear.Add(clearMessageDelay)) {
                        channelOpen = false;
                        audioPlayer.removeImmediateClip(folderHoldYourLine);
                        audioPlayer.removeImmediateClip(folderStillThere);
                        QueuedMessage clearMessage = new QueuedMessage(0, this);
                        clearMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + clearMessageExpiresAfter;
                        audioPlayer.playClipImmediately(folderClear, clearMessage);
                        audioPlayer.closeChannel();
                    }
                    timeWhenWeThinkWeAreClear = now;
                }
                else if ((carAlongSideInFront && carAlongSideInFrontPrevious && Math.Abs(closingSpeedInFront) < maxClosingSpeed) ||
                    (carAlongSideBehindPrevious && carAlongSideBehind && Math.Abs(closingSpeedBehind) < maxClosingSpeed))
                {                    
                    Boolean frontOverlapIsReducing = carAlongSideInFront && closingSpeedInFront > 0;
                    Boolean rearOverlapIsReducing =  carAlongSideBehind && closingSpeedBehind > 0;
                    if (channelOpen && now > timeOfLastHoldMessage.Add(repeatHoldFrequency))
                    {
                        // channel's already open, still there
                        Console.WriteLine("Still there...");
                        timeOfLastHoldMessage = now;
                        audioPlayer.removeImmediateClip(folderHoldYourLine);
                        audioPlayer.removeImmediateClip(folderClear);
                        QueuedMessage stillThereMessage = new QueuedMessage(0, this);
                        stillThereMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + holdMessageExpiresAfter;
                        audioPlayer.playClipImmediately(folderStillThere, stillThereMessage);
                    }
                    else if (!channelOpen &&
                        (rearOverlapIsReducing || (frontOverlapIsReducing && !spotterOnlyWhenBeingPassed)))
                    {
                        if (now > timeWhenWeThinkWeAreOverlapping.Add(overlapMessageDelay))
                        {
                            Console.WriteLine("race time = " + currentState.Player.GameSimulationTime);
                            if (carAlongSideInFront)
                            {
                                Console.WriteLine("new overlap in front, deltaFront = " + deltaFront + " time gap = " +
                                carLengthToUse / currentSpeed + " closing speed = " + closingSpeedInFront);
                            }
                            if (carAlongSideBehind)
                            {
                                Console.WriteLine("new overlap behind, deltaBehind = " + deltaBehind + " time gap = " +
                                carLengthToUse / currentSpeed + " closing speed = " + closingSpeedBehind);
                            }
                            timeOfLastHoldMessage = now;
                            channelOpen = true;
                            audioPlayer.removeImmediateClip(folderClear);
                            audioPlayer.removeImmediateClip(folderStillThere);
                            audioPlayer.openChannel();
                            QueuedMessage holdMessage = new QueuedMessage(0, this);
                            holdMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + holdMessageExpiresAfter;
                            audioPlayer.playClipImmediately(folderHoldYourLine, holdMessage);
                        }
                        timeWhenWeThinkWeAreOverlapping = now;
                    }              
                }
            }
            else if (channelOpen)
            {
                channelOpen = false;
                audioPlayer.closeChannel();
            }
        }

        // get the closing speed (> 0 if we're getting closer, < 0 if we're getting further away)
        private float getClosingSpeed(Shared lastState, Shared currentState, Boolean front)
        {
            // note that we always use current speed here. This is because the data are noisy and the
            // gap and speed data occasionally contain incorrect small values. If this happens to the 
            // currentSpeed, we'll already have discarded the data in this iteration (currentSpeed < minSpotterSpeed).
            // If the either of the timeDeltas are very small we'll either interpret this as a very high closing speed 
            // or a negative closing speed, neither of which should trigger a 'hold your line' message.

            // We really should be using the speed from the lastState when calculating the gap at the
            // lastState, but the speed should (if the data are correct) be fairly similar
            float timeElapsed = (float)currentState.Player.GameSimulationTime - (float)lastState.Player.GameSimulationTime;
            if (front)
            {
                return ((Math.Abs(lastState.TimeDeltaFront) * currentState.CarSpeed) - 
                    (Math.Abs(currentState.TimeDeltaFront) * currentState.CarSpeed)) / timeElapsed;
            } else
            {
                return ((Math.Abs(lastState.TimeDeltaBehind) * currentState.CarSpeed) -
                    (Math.Abs(currentState.TimeDeltaBehind) * currentState.CarSpeed)) / timeElapsed;
            }
        }
    }
}
