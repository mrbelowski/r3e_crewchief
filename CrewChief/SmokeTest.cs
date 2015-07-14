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
            /*audioPlayer.queueClip(folderTest, 0, this);
            
            audioPlayer.queueClip(QueuedMessage.compoundMessageIdentifier + "_lapTimeNotRaceTime",
                                    new QueuedMessage("lap_times/time_intro", null, TimeSpan.FromMilliseconds(new Random().Next(60000, 70500)), 0, this));
            audioPlayer.queueClip(QueuedMessage.compoundMessageIdentifier + "_lapTimeNotRaceGap",
                                        new QueuedMessage("lap_times/gap_intro", "lap_times/gap_outro_off_pace",
                                            TimeSpan.FromMilliseconds(new Random().Next(1000, 4000)), 5, this));*/
            Console.WriteLine("queuing test");
            audioPlayer.queueClip(folderTest, 0, this);
            Thread.Sleep(2000);
            Console.WriteLine("requesting open channel from client");
            audioPlayer.openChannel();
            Console.WriteLine("playing immediately from client");
            QueuedMessage holdMessage = new QueuedMessage(0, this);
            holdMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + 3000;
            audioPlayer.playClipImmediately("spotter/hold_your_line", holdMessage);
            Thread.Sleep(500);
            audioPlayer.queueClip("mandatory_pit_stops/pit_now", 0, this);
            Thread.Sleep(5000);
            QueuedMessage clearMessage = new QueuedMessage(0, this);
            clearMessage.expiryTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + 3000;
            audioPlayer.playClipImmediately("spotter/clear", clearMessage);
            audioPlayer.closeChannel();
        }
    }
}
