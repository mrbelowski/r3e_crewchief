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

        private String folderP10 = "position/p10";

        private int previousPosition;

        private Boolean eventHasFiredInThisSession;

        private Boolean playedMessageForOutsideTop10;

        private int startPosition;

        public Position(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        protected override void clearStateInternal()
        {
            previousPosition = 0;
            eventHasFiredInThisSession = false;
            playedMessageForOutsideTop10 = false;
            startPosition = 0;
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return true;
        }

        protected override void triggerInternal(Data.Shared lastState, Data.Shared currentState)
        {
            if (startPosition == 0 && currentState.Position > 0)
            {
                startPosition = currentState.Position;
            }
            if (isNewLap) {
                if (previousPosition == 0 && currentState.Position > 0) {
                    previousPosition = currentState.Position;
                } else {
                    if (!eventHasFiredInThisSession || previousPosition != currentState.Position) {
                        PearlsOfWisdom.PearlType pearlType = PearlsOfWisdom.PearlType.BAD;
                        Boolean isImproving = false;
                        if (previousPosition > currentState.Position)
                        {
                            pearlType = PearlsOfWisdom.PearlType.GOOD;
                            isImproving = true;
                        }
                        eventHasFiredInThisSession = currentState.Position <= 10;
                        Console.WriteLine("Position event: position at lap " + currentState.CompletedLaps + " = " + currentState.Position);
                        Boolean p10orBetter = true;
                        
                        switch (currentState.Position) {
                            case 1 :
                                audioPlayer.queueClip(folderP1, 0, this, pearlType, 0.5);
                                break;
                            case 2 :
                                audioPlayer.queueClip(folderP2, 0, this, pearlType, 0.5);
                                break;
                            case 3 :
                                audioPlayer.queueClip(folderP3, 0, this, pearlType, 0.5);
                                break;
                            case 4 :
                                audioPlayer.queueClip(folderP4, 0, this, pearlType, 0.5);
                                break;
                            case 5 :
                                audioPlayer.queueClip(folderP5, 0, this, pearlType, 0.5);
                                break;
                            case 6 :
                                audioPlayer.queueClip(folderP6, 0, this, pearlType, 0.5);
                                break;
                            case 7 :
                                audioPlayer.queueClip(folderP7, 0, this, pearlType, 0.5);
                                break;
                            case 8 :
                                audioPlayer.queueClip(folderP8, 0, this, pearlType, 0.5);
                                break;
                            case 9 :
                                audioPlayer.queueClip(folderP9, 0, this, pearlType, 0.5);
                                break;
                            case 10 :
                                audioPlayer.queueClip(folderP10, 0, this, pearlType, 0.5);
                                break;    
                            default :
                                p10orBetter = false;
                                break;
                        }
                        if (!p10orBetter && !playedMessageForOutsideTop10 && PearlsOfWisdom.enablePearlsOfWisdom)
                        {
                            if (startPosition > currentState.Position + 5)
                            {
                                // has made up 5 places, so even though we're outside the top ten give some encouragement
                                audioPlayer.queueClip(PearlsOfWisdom.folderKeepItUp, 0, this);
                                playedMessageForOutsideTop10 = true;
                            }
                            else
                            {
                                audioPlayer.queueClip(PearlsOfWisdom.folderMustDoBetter, 0, this);
                                playedMessageForOutsideTop10 = true;
                            }
                        }
                        previousPosition = currentState.Position;
                    }
                }
            }
        }
    }
}
