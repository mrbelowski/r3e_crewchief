using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChief.Data;

namespace CrewChief.Events
{
    // note this only works for DTM as non-DTM events don't have number of laps *or* a race time remaining
    // field in the data block
    class Timings : AbstractEvent
    {
        private String folderLapTimeStart = "timings/lap_time_report";

        private String folderGapInFrontIncreasing = "timings/gap_in_front_increasing";
        private String folderGapInFrontDecreasing = "timings/gap_in_front_decreasing";
        private String folderGapInFrontConstant = "timings/gap_in_front_constant";

        private String folderGapBehindIncreasing = "timings/gap_behind_increasing";
        private String folderGapBehingDecreasing = "timings/gap_behind_decreasing";
        private String folderGapBehindConstant = "timings/gap_behind_constant";

        private String folderSeconds = "timings/seconds";

        private String folderBeingHeldUp = "timings/being_held_up";
        private String folderBeingPressured = "timings/being_pressured";

        private List<float> gapsInFront;

        private List<float> gapsBehind;

        private GapStatus lastGapInFrontReport;

        private GapStatus lastGapBehindReport;

        private int positionAtLastSector;

        private int numCarsAtLastSector;

        private float gapBehindAtLastReport;

        private float gapInFrontAtLastReport;

        private int sectorsSinceLastReport;
        
        private int sectorsUntilNextReport;

        private Random rand = new Random();

        public Timings(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        protected override void clearStateInternal()
        {
            gapsInFront = new List<float>();
            gapsBehind = new List<float>();
            positionAtLastSector = 0;
            numCarsAtLastSector = 0;
            lastGapBehindReport = GapStatus.NONE;
            lastGapInFrontReport = GapStatus.NONE;
            gapBehindAtLastReport = -1;
            gapInFrontAtLastReport = -1;
            sectorsSinceLastReport = 0;
            sectorsUntilNextReport = 0;
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return isSessionRunning;
        }

        protected override void triggerInternal(Data.Shared lastState, Data.Shared currentState)
        {
            if (isRaceStarted && isNewSector)
            {
                sectorsSinceLastReport++;          
                // only report gaps if the player's position hasn't changed and the number of cars in the race hasn't changed. This is the
                // only way to be sure that we're reporting gaps to the same opponent car
                if (currentState.Position == positionAtLastSector && currentState.NumCars == numCarsAtLastSector)
                {
                    GapStatus gapInFrontStatus = GapStatus.NONE;
                    GapStatus gapBehindStatus = GapStatus.NONE;
                    if (currentState.Position != 1)
                    {
                        gapsInFront.Insert(0, currentState.TimeDeltaFront);
                        gapInFrontStatus = getGapStatus(gapsInFront, gapInFrontAtLastReport);
                        Console.WriteLine("Gap front " + gapInFrontStatus + ", " + gapsInFront[0]);
                    }

                    if (!isLast)
                    {
                        gapsBehind.Insert(0, currentState.TimeDeltaBehind);
                        gapBehindStatus = getGapStatus(gapsBehind, gapBehindAtLastReport);
                        Console.WriteLine("Gap behind " + gapBehindStatus + ", " + gapsBehind[0]);
                    }

                    // Play which ever is the smaller gap, but we're not interested if the gap is < 0.5 or > 20 seconds or hasn't changed:
                    Boolean playGapInFront = gapInFrontStatus != GapStatus.NONE && gapInFrontStatus != GapStatus.CONSTANT && 
                        (gapBehindStatus == GapStatus.NONE || gapsInFront[0] < gapsBehind[0]);

                    Boolean playGapBehind = !playGapInFront && gapBehindStatus != GapStatus.NONE && gapBehindStatus != GapStatus.CONSTANT;

                    if (playGapInFront && sectorsSinceLastReport >= sectorsUntilNextReport)
                    {
                        sectorsSinceLastReport = 0;
                        // here we report on gaps semi-randomly, we'll see how this sounds...
                        sectorsUntilNextReport = rand.Next(2, 6);
                        switch (gapInFrontStatus)
                        {
                            case GapStatus.INCREASING:
                                audioPlayer.queueClip(QueuedMessage.compoundMessageIdentifier + "Timings/gaps", new QueuedMessage(folderGapInFrontIncreasing, folderSeconds,
                                    TimeSpan.FromMilliseconds(gapsInFront[0] * 1000), 0, this));
                                lastGapInFrontReport = GapStatus.INCREASING;
                                gapInFrontAtLastReport = gapsInFront[0];
                                break;
                            case GapStatus.DECREASING:
                                audioPlayer.queueClip(QueuedMessage.compoundMessageIdentifier + "Timings/gaps", new QueuedMessage(folderGapInFrontDecreasing, folderSeconds,
                                    TimeSpan.FromMilliseconds(gapsInFront[0] * 1000), 0, this));
                                lastGapInFrontReport = GapStatus.DECREASING;
                                gapInFrontAtLastReport = gapsInFront[0];
                                break;
                            case GapStatus.CONSTANT:
                                // do we even want 'constant' reports?
                                //audioPlayer.queueClip(QueuedMessage.compoundMessageIdentifier + "Timings/gaps", new QueuedMessage(folderGapInFrontConstant, folderSeconds,
                                //    TimeSpan.FromMilliseconds(gapsInFront[0] * 1000), 0, this));
                                //lastGapInFrontReport = GapStatus.CONSTANT;
                                break;
                            case GapStatus.CLOSE:
                                audioPlayer.queueClip(folderBeingHeldUp, 0, this);
                                lastGapInFrontReport = GapStatus.CLOSE;
                                gapInFrontAtLastReport = gapsInFront[0];
                                break;
                        }
                    }
                    if (playGapBehind && sectorsSinceLastReport > sectorsUntilNextReport)
                    {
                        sectorsSinceLastReport = 0;
                        sectorsUntilNextReport = rand.Next(2, 6);
                        switch (gapBehindStatus)
                        {
                            case GapStatus.INCREASING:
                                audioPlayer.queueClip(QueuedMessage.compoundMessageIdentifier + "Timings/gaps", new QueuedMessage(folderGapBehindIncreasing, folderSeconds,
                                    TimeSpan.FromMilliseconds(gapsBehind[0] * 1000), 0, this));
                                lastGapBehindReport = GapStatus.INCREASING;
                                gapBehindAtLastReport = gapsBehind[0];
                                break;
                            case GapStatus.DECREASING:
                                audioPlayer.queueClip(QueuedMessage.compoundMessageIdentifier + "Timings/gaps", new QueuedMessage(folderGapInFrontDecreasing, folderSeconds,
                                    TimeSpan.FromMilliseconds(gapsBehind[0] * 1000), 0, this));
                                lastGapBehindReport = GapStatus.DECREASING;
                                gapBehindAtLastReport = gapsBehind[0];
                                break;
                            case GapStatus.CONSTANT:
                                // do we even want 'constant' reports?
                                //audioPlayer.queueClip(QueuedMessage.compoundMessageIdentifier + "Timings/gaps", new QueuedMessage(folderGapBehindConstant, folderSeconds,
                                //    TimeSpan.FromMilliseconds(gapsBehind[0] * 1000), 0, this));
                                //lastGapBehindReport = GapStatus.CONSTANT;
                                break;
                            case GapStatus.CLOSE:
                                audioPlayer.queueClip(folderBeingPressured, 0, this);
                                lastGapBehindReport = GapStatus.CLOSE;
                                gapBehindAtLastReport = gapsBehind[0];
                                break;
                        }
                    }                                    
                }
                else
                {
                    clearStateInternal();
                    positionAtLastSector = currentState.Position;
                    numCarsAtLastSector = currentState.NumCars;
                }
            }

            if (isSessionRunning && currentState.SessionType == (int)Constant.Session.Qualify && isNewLap && currentState.LapTimePrevious > 0)
            {
                PearlsOfWisdom.PearlType pearlType = PearlsOfWisdom.PearlType.BAD;
                if (currentState.Position < 4)
                {
                    pearlType = PearlsOfWisdom.PearlType.GOOD;
                }
                else if (currentState.Position >= 4)
                {
                    pearlType = PearlsOfWisdom.PearlType.NEUTRAL;
                }
                audioPlayer.queueClip(QueuedMessage.compoundMessageIdentifier + "Timings",
                    new QueuedMessage(folderLapTimeStart, null, TimeSpan.FromSeconds(currentState.LapTimePrevious), 0, this), pearlType, 0.5);
            }
        }

        private GapStatus getGapStatus(List<float> gaps, float lastReportedGap)
        {
            // if we only have 1 gap in the list, or the last gap is too big, or the change in the gap is too big,
            // we don't want to report anything

            // when comparing gaps round to 1 decimal place
            if (gaps[0] == -1 || gaps.Count < 2 || gaps[0] > 20 || Math.Abs(gaps[0] - gaps[1]) > 5)
            {
                return GapStatus.NONE;
            }
            else if (gaps[0] < 0.5 && gaps[1] < 0.5) 
            {
                // this car has been close for 2 sectors
                return GapStatus.CLOSE;
            }
            else if (lastReportedGap == -1)
            {                
                if (Math.Round(gaps[0], 1) > Math.Round(gaps[1], 1))
                {
                    return GapStatus.INCREASING;
                }
                else if (Math.Round(gaps[0], 1) < Math.Round(gaps[1], 1))
                {
                    return GapStatus.DECREASING;
                }
                else
                {
                    return GapStatus.CONSTANT;
                }
            }
            else
            {
                if (Math.Round(gaps[0], 1) > Math.Round(lastReportedGap))
                {
                    return GapStatus.INCREASING;
                }
                else if (Math.Round(gaps[0], 1) < Math.Round(lastReportedGap))
                {
                    return GapStatus.DECREASING;
                }
                else
                {
                    return GapStatus.CONSTANT;
                }
            }           
        }

        private enum GapStatus
        {
            CLOSE, CONSTANT, INCREASING, DECREASING, NONE
        }
    }
}
