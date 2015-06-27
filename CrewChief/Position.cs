using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChief.Data;

namespace CrewChief.Events
{
    class Position : AbstractEvent
    {
        private String folderP1 = "position/p1";

        private String folderP2 = "position/p2";

        private String folderP3 = "position/p3";

        private String folderP4 = "position/p4";

        private String folderP5 = "position/p5";

        private String folderP6 = "position/p6";

        private String folderP7 = "position/p7";

        private String folderP8 = "position/p8";

        private String folderP9 = "position/p9";

        private String folderP10 = "position/p1";

        private int previousPosition;

        public Position(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        protected override void clearStateInternal()
        {
            previousPosition = 0;
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return true;
        }

        protected override void triggerInternal(Data.Shared lastState, Data.Shared currentState)
        {
            if (isNewLap) {
                if (previousPosition < 1 && currentState.Position > 0) {
                    previousPosition = currentState.Position;
                } else {
                    if (previousPosition != currentState.Position) {
                        switch (currentState.Position) {
                            case 1 :
                                audioPlayer.queueClip(folderP1, 0, this);
                                break;
                            case 2 :
                                audioPlayer.queueClip(folderP2, 0, this);
                                break;
                            case 3 :
                                audioPlayer.queueClip(folderP3, 0, this);
                                break;
                            case 4 :
                                audioPlayer.queueClip(folderP4, 0, this);
                                break;
                            case 5 :
                                audioPlayer.queueClip(folderP5, 0, this);
                                break;
                            case 6 :
                                audioPlayer.queueClip(folderP6, 0, this);
                                break;
                            case 7 :
                                audioPlayer.queueClip(folderP7, 0, this);
                                break;
                            case 8 :
                                audioPlayer.queueClip(folderP8, 0, this);
                                break;
                            case 9 :
                                audioPlayer.queueClip(folderP9, 0, this);
                                break;
                            case 10 :
                                audioPlayer.queueClip(folderP10, 0, this);
                                break;
                            
                        }
                        previousPosition = currentState.Position;
                    }
                }
            }
        }
    }
}
