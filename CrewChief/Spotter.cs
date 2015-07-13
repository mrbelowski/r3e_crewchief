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
        private float carLength = 4.5f;
        private Boolean channelOpen;

        private int repeatHoldFrequency = 3;
        private String folderClear = "spotter/clear";
        private String folderHoldYourLine = "spotter/hold_your_line";

        public Spotter(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        protected override void clearStateInternal()
        {
            channelOpen = false;
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

            if (isRaceStarted && speed > 10)
            {
                Boolean carAlongSide = (deltaFront > -1 && carLength / speed > deltaFront) || 
                    (deltaBehind > -1 && carLength / speed > deltaBehind);
                
                if (channelOpen && !carAlongSide)
                {
                    channelOpen = false;
                    audioPlayer.playClipImmediately(folderClear, new QueuedMessage(0, this));
                    audioPlayer.closeChannel();
                } 
                else if (!channelOpen && carAlongSide)
                {
                    channelOpen = true;
                    audioPlayer.openChannel();
                    // todo: repeats
                    audioPlayer.playClipImmediately(folderHoldYourLine, new QueuedMessage(0, this));
                }
            }
            else if (channelOpen)
            {
                channelOpen = false;
                audioPlayer.closeChannel();
            }
        }
    }
}
