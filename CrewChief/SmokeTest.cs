﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChief.Data;
using System.Threading;

namespace CrewChief.Events
{
    class SmokeTest : AbstractEvent
    {
        private String folderTest = "radio_check/test";

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
            audioPlayer.queueClip(folderTest, 0, this);            
        }
    }
}
