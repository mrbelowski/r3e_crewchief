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

        // only report changing gaps if we've got at least 4 logged gaps *to the same car*
        private int gapsWindowSize = 4;

        private List<float> gapsInFront;

        private List<float> gapsBehind;

        private GapStatus lastGapInFrontReport;

        private GapStatus lastGapBehindReport;

        private int positionAtLastSector;

        private int numCarsAtLastSector;

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
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return isSessionRunning;
        }

        protected override void triggerInternal(Data.Shared lastState, Data.Shared currentState)
        {
            if (isRaceStarted && isNewSector)
            {
                if (currentState.Position == positionAtLastSector && currentState.NumCars == numCarsAtLastSector)
                {
                    if (currentState.Position != 1)
                    {
                        gapsInFront.Insert(0, currentState.TimeDeltaFront);
                    }

                    if (!isLast)
                    {
                        gapsBehind.Insert(0, currentState.TimeDeltaBehind);
                    }
                    GapStatus gapInFrontStatus = getGapStatus(gapsInFront);
                    GapStatus gapBehindStatus = getGapStatus(gapsBehind);

                    // play which ever status has changed. If they've both changed, play which ever is the smaller gap:
                    Boolean playGapInFront = gapInFrontStatus != GapStatus.NONE && gapInFrontStatus != lastGapInFrontReport &&
                        (gapBehindStatus == GapStatus.NONE || gapBehindStatus == lastGapBehindReport || gapsInFront[0] < gapsBehind[0]);

                    Boolean playGapBehind = !playGapInFront && gapBehindStatus != GapStatus.NONE && gapBehindStatus != lastGapBehindReport;

                    if (playGapInFront)
                    { 
                        switch (gapInFrontStatus) {
                            case GapStatus.INCREASING:
                                audioPlayer.queueClip(QueuedMessage.compoundMessageIdentifier + "Timings/gaps", new QueuedMessage(folderGapInFrontIncreasing, folderSeconds,
                                    TimeSpan.FromMilliseconds(gapsInFront[0] * 1000), 0, this));
                                lastGapInFrontReport = GapStatus.INCREASING;
                            break;
                            case GapStatus.DECREASING:
                                audioPlayer.queueClip(QueuedMessage.compoundMessageIdentifier + "Timings/gaps", new QueuedMessage(folderGapInFrontDecreasing, folderSeconds,
                                    TimeSpan.FromMilliseconds(gapsInFront[0] * 1000), 0, this));
                                lastGapInFrontReport = GapStatus.DECREASING;
                            break;
                            case GapStatus.CONSTANT:
                                // do we even want 'constant' reports?
                                //audioPlayer.queueClip(QueuedMessage.compoundMessageIdentifier + "Timings/gaps", new QueuedMessage(folderGapInFrontConstant, folderSeconds,
                                //    TimeSpan.FromMilliseconds(gapsInFront[0] * 1000), 0, this));
                                //lastGapInFrontReport = GapStatus.CONSTANT;
                            break;
                        }
                    }
                    if (playGapBehind)
                    {
                        switch (gapBehindStatus)
                        {
                            case GapStatus.INCREASING:
                                audioPlayer.queueClip(QueuedMessage.compoundMessageIdentifier + "Timings/gaps", new QueuedMessage(folderGapBehindIncreasing, folderSeconds,
                                    TimeSpan.FromMilliseconds(gapsBehind[0] * 1000), 0, this));
                                lastGapBehindReport = GapStatus.INCREASING;
                                break;
                            case GapStatus.DECREASING:
                                audioPlayer.queueClip(QueuedMessage.compoundMessageIdentifier + "Timings/gaps", new QueuedMessage(folderGapInFrontDecreasing, folderSeconds,
                                    TimeSpan.FromMilliseconds(gapsBehind[0] * 1000), 0, this));
                                lastGapBehindReport = GapStatus.DECREASING;
                                break;
                            case GapStatus.CONSTANT:
                                // do we even want 'constant' reports?
                                //audioPlayer.queueClip(QueuedMessage.compoundMessageIdentifier + "Timings/gaps", new QueuedMessage(folderGapBehindConstant, folderSeconds,
                                //    TimeSpan.FromMilliseconds(gapsBehind[0] * 1000), 0, this));
                                //lastGapBehindReport = GapStatus.CONSTANT;
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
            
            if (isSessionRunning && currentState.SessionType == (int) Constant.Session.Qualify && isNewLap)
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
                    new QueuedMessage(folderLapTimeStart, folderSeconds, TimeSpan.FromSeconds(currentState.LapTimePrevious), 0, this), pearlType, 0.5);
            }
        }

        private GapStatus getGapStatus(List<float> gaps)
        {
            if (gaps.Count >= gapsWindowSize)
            {
                if (Math.Round(gaps[0], 1) == Math.Round(gapsInFront[gapsWindowSize - 1], 1))
                {
                    return GapStatus.CONSTANT;
                }
                if (Math.Round(gaps[0], 1) > Math.Round(gapsInFront[gapsWindowSize - 1], 1))
                {
                    return GapStatus.INCREASING;
                }
                if (Math.Round(gaps[0], 1) < Math.Round(gapsInFront[gapsWindowSize - 1], 1))
                {
                    return GapStatus.DECREASING;
                }
            }
            return GapStatus.NONE;
        }

        private enum GapStatus
        {
            CONSTANT, INCREASING, DECREASING, NONE
        }
    }
}
