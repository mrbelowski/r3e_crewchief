using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChief.Data;

namespace CrewChief
{
    class PearlsOfWisdom
    {
        public static Boolean enablePearlsOfWisdom = Properties.Settings.Default.enable_pearls_of_wisdom;
        public static String folderMustDoBetter = "pearls_of_wisdom/must_do_better";
        public static String folderKeepItUp = "pearls_of_wisdom/keep_it_up";
        public static String folderNeutral = "pearls_of_wisdom/neutral";

        private Random random = new Random();

        public enum PearlType
        {
            GOOD, BAD, NEUTRAL, NONE
        }

        public enum PearlMessagePosition
        {
            BEFORE, AFTER, NONE
        }

        public PearlMessagePosition getMessagePosition(double messageProbability)
        {
            if (enablePearlsOfWisdom && messageProbability > random.NextDouble())
            {
                if (random.Next(0, 1) > 0.5)
                {
                    return PearlMessagePosition.BEFORE;
                }
                else
                {
                    return PearlMessagePosition.AFTER;
                }
            }
            return PearlMessagePosition.NONE;
        }

        public static String getMessageFolder(PearlType pearlType)
        {
            switch (pearlType) {
                case PearlType.GOOD:
                    return folderKeepItUp;
                case PearlType.BAD:
                    return folderMustDoBetter;
                case PearlType.NEUTRAL:
                    return folderNeutral;
                default:
                    Console.WriteLine("Error getting pearl type for type " + pearlType);
                    return "";                
            }
        }
    }
}
