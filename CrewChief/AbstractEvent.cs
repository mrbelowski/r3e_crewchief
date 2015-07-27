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

        protected PearlsOfWisdom pearlsOfWisdom;

        // this is called on each 'tick' (currently every 2 seconds) - the event subtype should
        // place its logic in here including calls to audioPlayer.queueClip
        abstract protected void triggerInternal(Shared lastState, Shared currentState);

        // reinitialise any state held by the event subtype
        public abstract void clearState();

        // generally the event subclass can just return true for this, but when a clip is played with
        // a non-zero delay it may be necessary to re-check that the clip is still valid against the current
        // state
        abstract public Boolean isClipStillValid(String eventSubType);

        public void respond()
        {
            // no-op, override in the subclasses
        }

        public void setPearlsOfWisdom(PearlsOfWisdom pearlsOfWisdom)
        {
            this.pearlsOfWisdom = pearlsOfWisdom;
        }

        public void trigger(Shared lastState, Shared currentState) 
        {
            // don't trigger events if someone else is driving the car (or it's a replay). No way to tell the difference between
            // watching an AI 'live' and the AI being in control of your car for starts / pitstops
            if (!CommonData.isNew && currentState.ControlType != (int)Constant.Control.Remote && 
                currentState.ControlType != (int)Constant.Control.Replay)
            {
                triggerInternal(lastState, currentState);
            }
        }
    }
}
