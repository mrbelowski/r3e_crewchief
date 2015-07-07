using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChief.Data;

namespace CrewChief.Events
{
    abstract class AbstractEvent
    {
        protected AudioPlayer audioPlayer;

        protected Boolean isNewLap;
            
        protected Boolean isNew;

        protected Boolean isRaceStarted;

        protected Boolean isSessionRunning;

        protected PearlsOfWisdom pearlsOfWisdom;

        // note this will be -1 if we don't actually know what sector we're in
        protected int currentLapSector;

        // this is called on each 'tick' (currently every 2 seconds) - the event subtype should
        // place its logic in here including calls to audioPlayer.queueClip
        abstract protected void triggerInternal(Shared lastState, Shared currentState);

        // reinitialise any state held by the event subtype
        abstract protected void clearStateInternal();

        // generally the event subclass can just return true for this, but when a clip is played with
        // a non-zero delay it may be necessary to re-check that the clip is still valid against the current
        // state
        abstract public Boolean isClipStillValid(String eventSubType);

        public void clearState()
        {
            isNew = true;
            clearStateInternal();
            currentLapSector = -1;
        }

        public void setPearlsOfWisdom(PearlsOfWisdom pearlsOfWisdom)
        {
            this.pearlsOfWisdom = pearlsOfWisdom;
        }

        public void trigger(Shared lastState, Shared currentState) 
        {
            getCommonStateData(lastState, currentState);
            // don't trigger events if someone else is driving the car (or it's a replay). No way to tell the difference between
            // watching an AI 'live' and the AI being in control of your car for starts / pitstops
            if (!isNew && currentState.ControlType != (int)Constant.Control.Remote && currentState.ControlType != (int)Constant.Control.Replay)
            {
                triggerInternal(lastState, currentState);
            }
            isNew = false;
        }

        private void getCommonStateData(Shared lastState, Shared currentState)
        {            
            isNewLap = isNew || (currentState.CompletedLaps > 0 && lastState.CompletedLaps < currentState.CompletedLaps);
            isRaceStarted = currentState.SessionPhase == (int)Constant.SessionPhase.Green && currentState.SessionType == (int) Constant.Session.Race;
            isSessionRunning = currentState.SessionPhase == (int)Constant.SessionPhase.Green;

            // TODO: here we're assuming that when we start a new lap the sector deltas aren't zeroed If they
            // are these if blocks should be 
            //if (currentLapSector == 1 && lastState.SectorTimeDeltaSelf.Sector1 == 0 && currentState.SectorTimeDeltaSelf.Sector1 != 0)
            if (isNewLap && currentLapSector != 1) {
                currentLapSector = 1;
            } else if (currentLapSector == 1 && lastState.SectorTimeDeltaSelf.Sector1 != currentState.SectorTimeDeltaSelf.Sector1)
            {
                currentLapSector = 2;
            } else if (currentLapSector == 2 && lastState.SectorTimeDeltaSelf.Sector2 != currentState.SectorTimeDeltaSelf.Sector2) 
            {
                currentLapSector = 3;
            } 
        }
    }
}
