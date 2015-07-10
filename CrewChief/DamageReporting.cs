using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChief.Data;

namespace CrewChief.Events
{
    class DamageReporting : AbstractEvent
    {

        private String folderMinorTransmissionDamage = "damage_reporting/minor_transmission_damage";
        private String folderMinorEngineDamage = "damage_reporting/minor_engine_damage";
        private String folderMinorAeroDamage = "damage_reporting/minor_aero_damage";

        private String folderSevereTransmissionDamage = "damage_reporting/severe_transmission_damage";
        private String folderSevereEngineDamage = "damage_reporting/severe_engine_damage";
        private String folderSevereAeroDamage = "damage_reporting/severe_aero_damage";

        private String folderBustedTransmission = "damage_reporting/busted_transmission";
        private String folderBustedEngine = "damage_reporting/busted_engine";

        Boolean playedMinorTransmissionDamage;
        Boolean playedMinorEngineDamage;
        Boolean playedMinorAeroDamage;
        Boolean playedSevereTransmissionDamage;
        Boolean playedSevereEngineDamage;
        Boolean playedSevereAeroDamage;
        Boolean playedBustedTransmission;
        Boolean playedBustedEngine;

        float minorTransmissionDamageThreshold = 0.97f;
        float minorEngineDamageThreshold = 0.97f;
        float minorAeroDamageThreshold = 0.97f;

        float severeTransmissionDamageThreshold = 0.4f;
        float severeEngineDamageThreshold = 0.4f;
        float severeAeroDamageThreshold = 0.7f;

        float bustedTransmissionThreshold = 0.0f;
        float bustedEngineThreshold = 0.0f;

        Boolean damageEnabled;

        public DamageReporting(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
            damageEnabled = false;
        }

        protected override void clearStateInternal()
        {
            playedMinorTransmissionDamage = false; playedMinorEngineDamage = false; playedMinorAeroDamage = false; playedSevereAeroDamage = false;
            playedSevereTransmissionDamage = false; playedSevereEngineDamage = false; playedBustedTransmission = false; playedBustedEngine = false;
        }

        public override bool isClipStillValid(string eventSubType)
        {
            return true;
        }

        override protected void triggerInternal(Shared lastState, Shared currentState)
        {
            if (!damageEnabled && currentState.CarDamage.Aerodynamics == 1 && 
                currentState.CarDamage.Transmission == 1 && currentState.CarDamage.Engine == 1)
            {
                Console.WriteLine("Damage is enabled...");
                damageEnabled = true;
            }
            // sanity check...
            if (damageEnabled && currentState.CarDamage.Aerodynamics == -1 &&
                currentState.CarDamage.Transmission == -1 && currentState.CarDamage.Engine == -1)
            {
                Console.WriteLine("Actually, damage is disabled...");
                damageEnabled = false;
                return;
            }
            if (isNewLap)
            {
                Console.WriteLine("Aero " + currentState.CarDamage.Aerodynamics);
                Console.WriteLine("Engine " + currentState.CarDamage.Engine);
                Console.WriteLine("Tranny " + currentState.CarDamage.Transmission);
            }
            if (damageEnabled)
            {
                if (!playedBustedEngine && currentState.CarDamage.Engine <= bustedEngineThreshold)
                {
                    audioPlayer.queueClip(folderBustedEngine, 0, this);
                    playedBustedEngine = true;
                    playedSevereEngineDamage = true;
                    playedMinorEngineDamage = true;
                    // if we've busted our engine, don't moan about other damage
                    playedBustedTransmission = true;
                    playedSevereTransmissionDamage = true;
                    playedMinorTransmissionDamage = true;
                    playedSevereAeroDamage = true;
                    playedMinorAeroDamage = true;
                }
                else if (!playedSevereEngineDamage && currentState.CarDamage.Engine <= severeEngineDamageThreshold)
                {
                    audioPlayer.queueClip(folderSevereEngineDamage, 5, this);
                    playedSevereEngineDamage = true;
                    playedMinorEngineDamage = true;
                }
                else if (!playedMinorEngineDamage && currentState.CarDamage.Engine <= minorEngineDamageThreshold)
                {
                    audioPlayer.queueClip(folderMinorEngineDamage, 5, this);
                    playedMinorEngineDamage = true;
                }

                if (!playedBustedTransmission && currentState.CarDamage.Transmission <= bustedTransmissionThreshold)
                {
                    audioPlayer.queueClip(folderBustedTransmission, 5, this);
                    playedBustedTransmission = true;
                    playedSevereTransmissionDamage = true;
                    playedMinorTransmissionDamage = true;
                    // if we've busted out transmission, don't moan about aero
                    playedSevereAeroDamage = true;
                    playedMinorAeroDamage = true;
                }
                else if (!playedSevereTransmissionDamage && currentState.CarDamage.Transmission <= severeTransmissionDamageThreshold)
                {
                    audioPlayer.queueClip(folderSevereTransmissionDamage, 5, this);
                    playedSevereTransmissionDamage = true;
                    playedMinorTransmissionDamage = true;
                }
                else if (!playedMinorTransmissionDamage && currentState.CarDamage.Transmission <= minorTransmissionDamageThreshold)
                {
                    audioPlayer.queueClip(folderMinorTransmissionDamage, 5, this);
                    playedMinorTransmissionDamage = true;
                }

                if (!playedSevereAeroDamage && currentState.CarDamage.Aerodynamics <= severeAeroDamageThreshold)
                {
                    audioPlayer.queueClip(folderSevereAeroDamage, 5, this);
                    playedSevereAeroDamage = true;
                    playedMinorAeroDamage = true;
                }
                else if (!playedMinorAeroDamage && currentState.CarDamage.Aerodynamics <= minorAeroDamageThreshold)
                {
                    audioPlayer.queueClip(folderMinorAeroDamage, 5, this);
                    playedMinorAeroDamage = true;
                }
            }
        }
    }
}
