using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChief.Data;

namespace CrewChief.Events
{
    class Position : AbstractEvent
    {
        public static String folderLeading = "position/leading";
        public static String folderPole = "position/pole";
        private String folderStub = "position/p";
        private String folderConsistentlyLast = "position/consistently_last";
        private String folderLast = "position/last";

        private int previousPosition;

        private int lapNumberAtLastMessage;

        private Random rand = new Random();

        private int numberOfLapsInLastPlace;

        public Position(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            previousPosition = 0;
            lapNumberAtLastMessage = 0;
            numberOfLapsInLastPlace = 0;
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return CommonData.isSessionRunning;
        }

        protected override void triggerInternal(Data.Shared lastState, Data.Shared currentState)
        {
            if (previousPosition == 0 && currentState.Position > 0)
            {
                previousPosition = currentState.Position;
            }
            if (CommonData.isNewLap && CommonData.isSessionRunning)
            {
                if (CommonData.isLast)
                {
                    numberOfLapsInLastPlace++;
                }
                else
                {
                    numberOfLapsInLastPlace = 0;
                }
                if (previousPosition == 0 && currentState.Position > 0) {
                    previousPosition = currentState.Position;
                } else {
                    if (currentState.NumberOfLaps > lapNumberAtLastMessage + 3
                            || previousPosition != currentState.Position) {
                        PearlsOfWisdom.PearlType pearlType = PearlsOfWisdom.PearlType.NONE;
                        float pearlLikelihood = 0.2f;
                        if (CommonData.isRaceStarted)
                        {
                            if (!CommonData.isLast && (previousPosition > currentState.Position + 5 || 
                                (previousPosition > currentState.Position && currentState.Position <= 5)))
                            {
                                pearlType = PearlsOfWisdom.PearlType.GOOD;
                                pearlLikelihood = 0.8f;
                            }
                            else if (!CommonData.isLast && previousPosition < currentState.Position && currentState.Position > 5)
                            {
                                // note that we don't play a pearl for being last - there's a special set of 
                                // insults reserved for this
                                pearlType = PearlsOfWisdom.PearlType.BAD;
                                pearlLikelihood = 0.5f;
                            }
                            else if (!CommonData.isLast)
                            {
                                pearlType = PearlsOfWisdom.PearlType.NEUTRAL;
                            }
                        }
                        Console.WriteLine("Position event: position at lap " + currentState.CompletedLaps + " = " + currentState.Position);
                        if (currentState.Position == 1)
                        {
                            if (currentState.SessionType == (int)Constant.Session.Race)
                            {
                                audioPlayer.queueClip(folderLeading, 0, this, pearlType, pearlLikelihood);
                            }
                            else if (currentState.SessionType == (int)Constant.Session.Practice)
                            {
                                audioPlayer.queueClip(folderStub + 1, 0, this, pearlType, pearlLikelihood);
                            }
                            // no p1 for pole - this is in the laptime tracker (yuk)
                        }
                        else if (!CommonData.isLast)
                        {
                            audioPlayer.queueClip(folderStub + currentState.Position, 0, this, pearlType, pearlLikelihood);
                        }
                        else if (CommonData.isLast)
                        {
                            if (numberOfLapsInLastPlace > 3)
                            {
                                audioPlayer.queueClip(folderConsistentlyLast, 0, this, PearlsOfWisdom.PearlType.NONE, 0);
                            }
                            else
                            {
                                audioPlayer.queueClip(folderLast, 0, this, PearlsOfWisdom.PearlType.NONE, 0);
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
