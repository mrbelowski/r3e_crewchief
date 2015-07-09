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

        private int positionAtLastP10OrWorseMessage;

        private int lapNumberAtLastMessage;

        public Position(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        protected override void clearStateInternal()
        {
            previousPosition = 0;
            lapNumberAtLastMessage = 0;
            positionAtLastP10OrWorseMessage = 0;
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return isSessionRunning;
        }

        protected override void triggerInternal(Data.Shared lastState, Data.Shared currentState)
        {
            if (previousPosition == 0 && currentState.Position > 0)
            {
                previousPosition = currentState.Position;
                positionAtLastP10OrWorseMessage = currentState.Position;
            }
            if (isNewLap && isSessionRunning) {
                if (previousPosition == 0 && currentState.Position > 0) {
                    previousPosition = currentState.Position;
                } else {
                    if (currentState.NumberOfLaps > lapNumberAtLastMessage + 3
                            || previousPosition != currentState.Position || currentState.SessionType == (int) Constant.Session.Qualify) {
                        PearlsOfWisdom.PearlType pearlType = PearlsOfWisdom.PearlType.NONE;
                        if (isRaceStarted)
                        {
                            if (!isLast && (previousPosition > currentState.Position + 5 || (previousPosition > currentState.Position && currentState.Position <= 5)))
                            {
                                pearlType = PearlsOfWisdom.PearlType.GOOD;
                            }
                            else if (!isLast && previousPosition < currentState.Position && currentState.Position > 5)
                            {
                                pearlType = PearlsOfWisdom.PearlType.BAD;
                            }
                            else
                            {
                                pearlType = PearlsOfWisdom.PearlType.NEUTRAL;
                            }
                        }
                        Console.WriteLine("Position event: position at lap " + currentState.CompletedLaps + " = " + currentState.Position);
                        Boolean p10orBetter = true;
                        
                        switch (currentState.Position) {
                            case 1 :
                                audioPlayer.queueClip(folderP1, 0, this, pearlType, 0.8);
                                break;
                            case 2 :
                                audioPlayer.queueClip(folderP2, 0, this, pearlType, 0.7);
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
                        // if we're outside the top ten, maybe play a pearl of wisdom - 50/50 chance, 
                        // scaled by the global multiplier
                        if ((isRaceStarted || currentState.NumberOfLaps > lapNumberAtLastMessage + 3) &&
                            PearlsOfWisdom.enablePearlsOfWisdom && 
                            !p10orBetter && new Random().NextDouble() > 0.5 * PearlsOfWisdom.pearlsLikelihood)
                        {
                            if (!isLast && positionAtLastP10OrWorseMessage > currentState.Position + 5)
                            {
                                // made up 5 places since last message
                                audioPlayer.queueClip(PearlsOfWisdom.folderKeepItUp, 0, this);
                                positionAtLastP10OrWorseMessage = currentState.Position;
                            }
                            else if (isLast || positionAtLastP10OrWorseMessage < currentState.Position - 1)
                            {
                                // lost 2 or more places since last message
                                audioPlayer.queueClip(PearlsOfWisdom.folderMustDoBetter, 0, this);
                                positionAtLastP10OrWorseMessage = currentState.Position;
                            }
                            else
                            {
                                audioPlayer.queueClip(PearlsOfWisdom.folderNeutral, 0, this);
                                positionAtLastP10OrWorseMessage = currentState.Position;
                            }
                        }
                        previousPosition = currentState.Position;
                        lapNumberAtLastMessage = currentState.NumberOfLaps;
                    }
                }
            }
        }
    }
}
