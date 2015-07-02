using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChief.Data;

namespace CrewChief.Events
{
    class SmokeTest : AbstractEvent
    {
        private String folderTest = "smoke_test/test";

        public SmokeTest(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        protected override void clearStateInternal()
        {
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return true;
        }

        override protected void triggerInternal(Shared lastState, Shared currentState)
        {
            audioPlayer.queueClip(folderTest, 0, this, PearlsOfWisdom.PearlType.NEUTRAL, 1);
        }
    }
}
