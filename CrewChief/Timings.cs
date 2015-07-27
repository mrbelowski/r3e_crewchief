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
        private String folderGapInFrontIncreasing = "timings/gap_in_front_increasing";
        private String folderGapInFrontDecreasing = "timings/gap_in_front_decreasing";

        private String folderGapBehindIncreasing = "timings/gap_behind_increasing";
        private String folderGapBehindDecreasing = "timings/gap_behind_decreasing";

        private String folderSeconds = "timings/seconds";

        private String folderBeingHeldUp = "timings/being_held_up";
        private String folderBeingPressured = "timings/being_pressured";

        private List<float> gapsInFront;

        private List<float> gapsBehind;

        private GapStatus lastGapInFrontReport;

        private GapStatus lastGapBehindReport;

        private float gapBehindAtLastReport;

        private float gapInFrontAtLastReport;

        private int sectorsSinceLastReport;
        
        private int sectorsUntilNextReport;

        private Random rand = new Random();

        private int drsRange;

        private Boolean hasDRS;

        public Timings(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            gapsInFront = new List<float>();
            gapsBehind = new List<float>();
            lastGapBehindReport = GapStatus.NONE;
            lastGapInFrontReport = GapStatus.NONE;
            gapBehindAtLastReport = -1;
            gapInFrontAtLastReport = -1;
            sectorsSinceLastReport = 0;
            sectorsUntilNextReport = 0;
            drsRange = 2;  // TODO: get the DRS range from somewhere
            hasDRS = false;
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return CommonData.isSessionRunning;
        }

        protected override void triggerInternal(Data.Shared lastState, Data.Shared currentState)
        {
            if (!hasDRS && currentState.DrsAvailable == 1)
            {
                hasDRS = true;
            }
            if (CommonData.isRaceStarted && CommonData.isNewSector)
            {
                sectorsSinceLastReport++;
                if (!CommonData.racingSameCarInFront)
                {
                    gapsInFront.Clear();
                }
                if (!CommonData.racingSameCarBehind)
                {
                    gapsBehind.Clear();
                }
                GapStatus gapInFrontStatus = GapStatus.NONE;
                GapStatus gapBehindStatus = GapStatus.NONE;
                if (currentState.Position != 1)
                {
                    gapsInFront.Insert(0, currentState.TimeDeltaFront);
                    gapInFrontStatus = getGapStatus(gapsInFront, gapInFrontAtLastReport);
                }
                if (!CommonData.isLast)
                {
                    gapsBehind.Insert(0, currentState.TimeDeltaBehind);
                    gapBehindStatus = getGapStatus(gapsBehind, gapBehindAtLastReport);
                }

                // Play which ever is the smaller gap, but we're not interested if the gap is < 0.5 or > 20 seconds or hasn't changed:
                Boolean playGapInFront = gapInFrontStatus != GapStatus.NONE && 
                    (gapBehindStatus == GapStatus.NONE || gapsInFront[0] < gapsBehind[0]);

                Boolean playGapBehind = !playGapInFront && gapBehindStatus != GapStatus.NONE;

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
                            audioPlayer.queueClip(QueuedMessage.compoundMessageIdentifier + "Timings/gaps", new QueuedMessage(folderGapBehindDecreasing, folderSeconds,
                                TimeSpan.FromMilliseconds(gapsBehind[0] * 1000), 0, this));
                            lastGapBehindReport = GapStatus.DECREASING;
                            gapBehindAtLastReport = gapsBehind[0];
                            break;
                        case GapStatus.CLOSE:
                            audioPlayer.queueClip(folderBeingPressured, 0, this);
                            lastGapBehindReport = GapStatus.CLOSE;
                            gapBehindAtLastReport = gapsBehind[0];
                            break;
                    }
                }
            }
        }

        private GapStatus getGapStatus(List<float> gaps, float lastReportedGap)
        {
            // if we have less than 3 gaps in the list, or the last gap is too big, or the change in the gap is too big,
            // we don't want to report anything

            // when comparing gaps round to 1 decimal place
            if (gaps.Count < 3 || gaps[0] <= 0 || gaps[1] <= 0 || gaps[2] <= 0 || gaps[0] > 20 || Math.Abs(gaps[0] - gaps[1]) > 5)
            {
                return GapStatus.NONE;
            }
            else if (gaps[0] < 0.5 && gaps[1] < 0.5) 
            {
                // this car has been close for 2 sectors
                return GapStatus.CLOSE;
            }
            if ((lastReportedGap == -1 || Math.Round(gaps[0], 1) > Math.Round(lastReportedGap)) && 
                Math.Round(gaps[0], 1) > Math.Round(gaps[1], 1) && Math.Round(gaps[1], 1) > Math.Round(gaps[2], 1))
            {
                return GapStatus.INCREASING;
            }
            else if ((lastReportedGap == -1 || Math.Round(gaps[0], 1) < Math.Round(lastReportedGap)) && 
                Math.Round(gaps[0], 1) < Math.Round(gaps[1], 1) && Math.Round(gaps[1], 1) < Math.Round(gaps[2], 1))
            {
                return GapStatus.DECREASING;
            }
            else
            {
                return GapStatus.NONE;
            }           
        }

        private enum GapStatus
        {
            CLOSE, INCREASING, DECREASING, NONE
        }
    }
}
