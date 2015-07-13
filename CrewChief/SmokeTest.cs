using System;
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
            Thread.Sleep(1000);
            Console.WriteLine("**************************");
            audioPlayer.openChannel();
            Thread.Sleep(1000);
            audioPlayer.playClipImmediately(folderTest, new QueuedMessage(0, this));
            Thread.Sleep(1000);
            audioPlayer.closeChannel();
        }
    }
}
