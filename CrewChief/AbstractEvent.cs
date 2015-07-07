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

        protected PearlsOfWisdom pearlsOfWisdom;

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
        }

        public void setPearlsOfWisdom(PearlsOfWisdom pearlsOfWisdom)
        {
            this.pearlsOfWisdom = pearlsOfWisdom;
        }

        public void trigger(Shared lastState, Shared currentState) 
        {
            getCommonStateData(lastState, currentState);
            if (!isNew)
            {
                triggerInternal(lastState, currentState);
            }
            isNew = false;
        }

        private void getCommonStateData(Shared lastState, Shared currentState)
        {            
            isNewLap = isNew || (currentState.CompletedLaps > 0 && lastState.CompletedLaps < currentState.CompletedLaps);
            //TODO: get the current sector
            currentLapSector = 0;
            isRaceStarted = currentState.SessionPhase == (int)Constant.SessionPhase.Green && currentState.SessionType == (int) Constant.Session.Race;
        }
    }
}
