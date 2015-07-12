﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Media;
using CrewChief.Events;
using System.Windows.Media;
using System.Collections.Specialized;

namespace CrewChief
{
    class AudioPlayer
    {
        private Dictionary<String, List<SoundPlayer>> clips = new Dictionary<String, List<SoundPlayer>>();

        private int queueMonitorInterval = 1000;

        private String soundFolderName = Properties.Settings.Default.sound_files_path;

        private String backgroundFolderName = Properties.Settings.Default.background_sound_files_path;

        private float backgroundVolume = Properties.Settings.Default.background_volume;

        private readonly TimeSpan minTimeBetweenPearlsOfWisdom = TimeSpan.FromSeconds(Properties.Settings.Default.minimum_time_between_pearls_of_wisdom);

        private Boolean sweary = Properties.Settings.Default.use_sweary_messages;

        private Random random = new Random();
    
        private OrderedDictionary queuedClips = new OrderedDictionary();

        static object Lock = new object();

        List<String> enabledSounds = new List<String>();

        Boolean enableStartBleep = false;

        Boolean enableEndBleep = false;

        MediaPlayer backgroundPlayer;

        private String soundFilesPath;

        private String backgroundFilesPath;

        // TODO: sort looping callback out so we don't need this...
        private int backgroundLeadout = 30;

        public static String dtmPitWindowOpenBackground = "dtm_pit_window_open.wav";

        public static String dtmPitWindowClosedBackground = "dtm_pit_window_closed.wav";

        // only the monitor Thread can request a reload of the background wav file, so
        // the events thread will have to set these variables to ask for a reload
        private Boolean loadNewBackground = false;
        private String backgroundToLoad;

        // test clips are only played on startup when they're enabled
        private String[] testClips1 = { "position/p1" };

        private int testClipsDelay = 5000;

        private String[] testClips2 = { "fuel/half_distance_low_fuel", "race_time/twenty_five_minutes"};

        private PearlsOfWisdom pearlsOfWisdom;

        DateTime timeLastPearlOfWisdomPlayed = DateTime.UtcNow;

        public void initialise() {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                soundFilesPath = Path.Combine(Path.GetDirectoryName(
                                        System.Reflection.Assembly.GetEntryAssembly().Location), @"..\", @"..\", soundFolderName);
                backgroundFilesPath = Path.Combine(Path.GetDirectoryName(
                                        System.Reflection.Assembly.GetEntryAssembly().Location), @"..\", @"..\", backgroundFolderName);
            }
            else
            {
                soundFilesPath = Path.Combine(Path.GetDirectoryName(
                                        System.Reflection.Assembly.GetEntryAssembly().Location), soundFolderName);
                backgroundFilesPath = Path.Combine(Path.GetDirectoryName(
                                            System.Reflection.Assembly.GetEntryAssembly().Location), backgroundFolderName);
            }
            Console.WriteLine("Sound dir full path = " + soundFilesPath);
            Console.WriteLine("Background sound dir full path = " + backgroundFilesPath);
            pearlsOfWisdom = new PearlsOfWisdom();
            try
            {
                DirectoryInfo soundDirectory = new DirectoryInfo(soundFilesPath);
                Console.WriteLine(soundDirectory);
                FileInfo[] bleepFiles = soundDirectory.GetFiles();
                foreach (FileInfo bleepFile in bleepFiles)
                {
                    if (bleepFile.Name.EndsWith(".wav"))
                    {
                        if (bleepFile.Name.StartsWith("start"))
                        {
                            enableStartBleep = true;
                            openAndCacheClip("start_bleep", bleepFile.FullName);
                        }
                        else if (bleepFile.Name.StartsWith("end"))
                        {
                            enableEndBleep = true;
                            openAndCacheClip("end_bleep", bleepFile.FullName);
                        }
                    }
                }
                DirectoryInfo[] eventFolders = soundDirectory.GetDirectories();
                foreach (DirectoryInfo eventFolder in eventFolders)
                {
                    try {
                        Console.WriteLine("Got event folder " + eventFolder.Name);
                        DirectoryInfo[] eventDetailFolders = eventFolder.GetDirectories();
                        foreach (DirectoryInfo eventDetailFolder in eventDetailFolders)
                        {
                            Console.WriteLine("Got event detail subfolder " + eventDetailFolder.Name);
                            String fullEventName = eventFolder + "/" + eventDetailFolder;
                            try
                            {
                                FileInfo[] soundFiles = eventDetailFolder.GetFiles();
                                foreach (FileInfo soundFile in soundFiles)
                                {
                                    if (soundFile.Name.EndsWith(".wav") && (sweary || !soundFile.Name.StartsWith("sweary")))
                                    {
                                        Console.WriteLine("Got sound file " + soundFile.FullName);
                                        openAndCacheClip(eventFolder + "/" + eventDetailFolder, soundFile.FullName);
                                        if (!enabledSounds.Contains(fullEventName))
                                        {
                                            enabledSounds.Add(fullEventName);
                                        }
                                    }
                                }
                                if (!enabledSounds.Contains(fullEventName))
                                {
                                    Console.WriteLine("Event " + fullEventName + " has no sound files");
                                }
                            }
                            catch (DirectoryNotFoundException e)
                            {
                                Console.WriteLine("Event subfolder " + fullEventName + " not found");
                            }
                        }
                    }
                    catch (DirectoryNotFoundException e)
                    {
                        Console.WriteLine("Unable to find events folder");
                    }
                }
                
                // spawn a Thread to monitor the queue
                ThreadStart work = monitorQueue;
                Thread thread = new Thread(work);
                thread.Start();
                playTestClips();
            }
            catch (DirectoryNotFoundException e) {
                Console.WriteLine("Unable to find sounds directory - path: " + soundFolderName);
            }
        }

        private void playTestClips()
        {
            new SmokeTest(this).trigger(new Data.Shared(), new Data.Shared());
            // now queue some tests...
            if (Properties.Settings.Default.play_test_clips_on_startup)
            {
                if (testClips1.Length > 0)
                {
                    foreach (String testClip in testClips1)
                    {
                        queueClip(testClip, 0, null);
                    }
                }
                if (testClipsDelay > 0 && testClips2.Length > 0)
                {
                    Thread.Sleep(testClipsDelay);
                    foreach (String testClip in testClips2)
                    {
                        queueClip(testClip, 0, null);
                    }
                }
            }
        }

        public void setBackgroundSound(String backgroundSoundName)
        {
            backgroundToLoad = backgroundSoundName;
            loadNewBackground = true;
        }

        private void monitorQueue() {
            Console.WriteLine("Monitor starting");
            if (backgroundVolume > 0)
            {
                backgroundPlayer = new MediaPlayer();
                backgroundPlayer.MediaEnded += new EventHandler(backgroundPlayer_MediaEnded);
                if (backgroundVolume > 1)
                {
                    backgroundVolume = 1;
                }
                backgroundPlayer.Volume = backgroundVolume;
                setBackgroundSound(dtmPitWindowClosedBackground);
            }
            while (true) { 
                Thread.Sleep(queueMonitorInterval);
                long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                List<String> keysToRemove = new List<String>();
                List<String> keysToPlay = new List<String>();
                lock (Lock)
                {
                    if (backgroundVolume > 0 && loadNewBackground && backgroundToLoad != null)
                    {
                        Console.WriteLine("Setting background sounds file to  " + backgroundToLoad);
                        String path = Path.Combine(soundFilesPath, backgroundFilesPath, backgroundToLoad);
                        backgroundPlayer.Open(new System.Uri(path, System.UriKind.Absolute));
                        loadNewBackground = false;
                    }

                    foreach (String key in queuedClips.Keys)
                    {
                        QueuedMessage queuedMessage = (QueuedMessage)queuedClips[key];
                        if (queuedMessage.dueTime <= milliseconds)
                        {
                            if ((queuedMessage.abstractEvent == null || queuedMessage.abstractEvent.isClipStillValid(key)) &&
                                !keysToPlay.Contains(key) && (!queuedMessage.gapFiller || playGapFillerMessage()))
                            {
                                keysToPlay.Add(key);
                            }
                            else
                            {
                                Console.WriteLine("Clip " + key + " is not valid");
                            }
                            keysToRemove.Add(key);
                        }
                    }
                    if (keysToPlay.Count > 0)
                    {
                        playSounds(keysToPlay, false);
                    }
                    foreach (String key in keysToRemove)
                    {
                        Console.WriteLine("Removing {0} from queue", key);
                        queuedClips.Remove(key);
                    }
                }
            }
        }

        private Boolean playGapFillerMessage()
        {
            return queuedClips.Count == 1 || (queuedClips.Count == 2 && random.Next() > 0.5);
        }

        public void close() {
            foreach (KeyValuePair<string, List<SoundPlayer>> entry in clips)
            {
                foreach (SoundPlayer clip in entry.Value)
                {
                    clip.Stop();
                    clips.Remove(entry.Key);
                }
            }
        }

        public void queueClip(String eventName, int secondsDelay, AbstractEvent abstractEvent)
        {
            queueClip(eventName, secondsDelay, abstractEvent, PearlsOfWisdom.PearlType.NONE, 0);
        }

        // we pass in the event which triggered this clip so that we can query the event before playing the
        // clip to check if it's still valid against the latest game state. This is necessary for clips queued
        // with non-zero delays (e.g. you might have crossed the start / finish line between the clip being 
        // queued and it being played)
        public void queueClip(String eventName, int secondsDelay, AbstractEvent abstractEvent, 
            PearlsOfWisdom.PearlType pearlType, double pearlMessageProbability) {
            queueClip(eventName, new QueuedMessage(secondsDelay, abstractEvent), pearlType, pearlMessageProbability);
        }

        public void queueClip(String eventName, QueuedMessage queuedMessage)
        {
            queueClip(eventName, queuedMessage, PearlsOfWisdom.PearlType.NONE, 0);
        }

        public void queueClip(String eventName, QueuedMessage queuedMessage,  PearlsOfWisdom.PearlType pearlType, double pearlMessageProbability)
        {
            lock (Lock)
            {
                if (queuedClips.Contains(eventName))
                {
                    Console.WriteLine("Clip for event " + eventName + " is already queued, ignoring");
                    return;
                }
                else
                {
                    Console.WriteLine("Queuing clip for event " + eventName);
                    PearlsOfWisdom.PearlMessagePosition pearlPosition = PearlsOfWisdom.PearlMessagePosition.NONE;
                    if (pearlType != PearlsOfWisdom.PearlType.NONE && checkPearlOfWisdomValid(pearlType))
                    {
                        pearlPosition = pearlsOfWisdom.getMessagePosition(pearlMessageProbability);
                    }
                    if (pearlPosition == PearlsOfWisdom.PearlMessagePosition.BEFORE)
                    {
                        queuedClips.Add(PearlsOfWisdom.getMessageFolder(pearlType),
                            new QueuedMessage(queuedMessage.dueTime, queuedMessage.abstractEvent));
                    }
                    queuedClips.Add(eventName, queuedMessage);
                    if (pearlPosition == PearlsOfWisdom.PearlMessagePosition.AFTER)
                    {
                        queuedClips.Add(PearlsOfWisdom.getMessageFolder(pearlType),
                            new QueuedMessage(queuedMessage.dueTime, queuedMessage.abstractEvent));
                    }
                }
            }
        }

        public void removeQueuedClip(String eventName)
        {
            lock (Lock)
            {
                if (queuedClips.Contains(eventName))
                {
                    queuedClips.Remove(eventName);
                }
            }
        }

        /**Don't use this unless you *really* have to (like the Green Green Green message). It's only for 
         cases where the message has to be played *immediately*. This will skip the background sounds (because
         * the Th. */
        public void playClipImmediately(String eventName)
        {
            Console.WriteLine("Clip " + eventName + " is being forcably played with no queuing");
            List<String> eventNames = new List<string>();
            eventNames.Add(eventName);
            playSounds(eventNames, true);
        }
    
        // use blockBackground if this call to play comes from a different Thread to the queue monitor - 
        // currently only from the playClipImmediately method
        private void playSounds(List<String> eventNames, Boolean blockBackground) {
            if (eventNames.Count == 1 && clipIsPearlOfWisdom(eventNames[0]) && hasPearlJustBeenPlayed())
            {
                Console.WriteLine("Rejecting pearl of wisdom " + eventNames[0] + 
                    " because one has been played in the last " + minTimeBetweenPearlsOfWisdom + " seconds"); return;
            }
            Boolean oneOrMoreEventsEnabled = false;
            foreach (String eventName in eventNames) 
            {
                if ((eventName.StartsWith(QueuedMessage.compoundMessageIdentifier) && 
                    ((QueuedMessage) queuedClips[eventName]).isValid) || enabledSounds.Contains(eventName))
                {
                    oneOrMoreEventsEnabled = true;
                }
            }
            if (oneOrMoreEventsEnabled)
            {
                // this looks like we're doing it the wrong way round but there's a short
                // delay playing the event sound, so if we kick off the background before the bleep
                if (!blockBackground && backgroundVolume > 0)
                {
                    int backgroundDuration = 0;
                    int backgroundOffset = 0;
                    if (backgroundPlayer.NaturalDuration.HasTimeSpan)
                    {
                        backgroundDuration = (backgroundPlayer.NaturalDuration.TimeSpan.Minutes * 60) +
                            backgroundPlayer.NaturalDuration.TimeSpan.Seconds;
                        //Console.WriteLine("Duration from file is " + backgroundDuration);
                        backgroundOffset = random.Next(0, backgroundDuration - backgroundLeadout);
                    }
                    //Console.WriteLine("Background offset = " + backgroundOffset);
                    backgroundPlayer.Position = TimeSpan.FromSeconds(backgroundOffset);
                    backgroundPlayer.Play();
                }
                
                if (enableStartBleep)
                {
                    List<SoundPlayer> bleeps = clips["start_bleep"];
                    int bleepIndex = random.Next(0, bleeps.Count);
                    bleeps[bleepIndex].PlaySync();
                }
                foreach (String eventName in eventNames)
                {
                    if ((eventName.StartsWith(QueuedMessage.compoundMessageIdentifier) &&
                        ((QueuedMessage) queuedClips[eventName]).isValid) || enabledSounds.Contains(eventName))
                    {
                        if (clipIsPearlOfWisdom(eventName))
                        {
                            if (hasPearlJustBeenPlayed())
                            {
                                Console.WriteLine("Rejecting pearl of wisdom " + eventName +
                                    " because one has been played in the last " + minTimeBetweenPearlsOfWisdom + " seconds");
                                continue;
                            }
                            else
                            {
                                timeLastPearlOfWisdomPlayed = DateTime.UtcNow;
                            }
                        }
                        if (eventName.StartsWith(QueuedMessage.compoundMessageIdentifier))
                        {
                            foreach (String message in ((QueuedMessage) queuedClips[eventName]).getMessageFolders())
                            {
                                List<SoundPlayer> clipsList = clips[message];
                                int index = random.Next(0, clipsList.Count);
                                SoundPlayer clip = clipsList[index];
                                Console.WriteLine("playing the sound at position " + index + ", name = " + clip.SoundLocation);
                                clip.PlaySync();
                            }
                        }
                        else
                        {
                            List<SoundPlayer> clipsList = clips[eventName];
                            int index = random.Next(0, clipsList.Count);
                            SoundPlayer clip = clipsList[index];
                            Console.WriteLine("playing the sound at position " + index + ", name = " + clip.SoundLocation);
                            clip.PlaySync();
                        }                        
                    }
                    else
                    {
                        Console.WriteLine("Event " + eventName + " is disabled");
                    }
                }
                if (enableEndBleep)
                {
                    List<SoundPlayer> bleeps = clips["end_bleep"];
                    int bleepIndex = random.Next(0, bleeps.Count);
                    bleeps[bleepIndex].PlaySync();
                }
                if (!blockBackground && backgroundVolume > 0)
                {
                    backgroundPlayer.Stop();
                }   
            }
            else
            {
                Console.WriteLine("All events " + String.Join(",", eventNames) + " are disabled");
            }
            Console.WriteLine("finished playing");
        }
        
        private void openAndCacheClip(String eventName, String file) {
            SoundPlayer clip = new SoundPlayer(file);
            clip.Load();
            if (!clips.ContainsKey(eventName)) {
                clips.Add(eventName, new List<SoundPlayer>());
            }
            clips[eventName].Add(clip);
            Console.WriteLine("cached clip " + file + " into set "+ eventName);
        }

        private void backgroundPlayer_MediaEnded(object sender, EventArgs e)
        {
            Console.WriteLine("looping...");
            backgroundPlayer.Position = TimeSpan.FromMilliseconds(1);
        }

        // checks that another pearl isn't already queued. If one of the same type is already
        // in the queue this method just returns false. If a conflicting pearl is in the queue
        // this method removes it and returns false, so we don't end up with, for example, 
        // a 'keep it up' message in a block that contains a 'your lap times are worsening' message
        private Boolean checkPearlOfWisdomValid(PearlsOfWisdom.PearlType newPearlType)
        {
            Boolean isValid = true;
            if (queuedClips != null && queuedClips.Count > 0)
            {
                List<String> pearlsToPurge = new List<string>();
                foreach (String eventName in queuedClips.Keys)
                {
                    if (clipIsPearlOfWisdom(eventName))
                    {
                        Console.WriteLine("There's already a pearl in the queue, can't add anothner");
                        isValid = false;
                        if (eventName != PearlsOfWisdom.getMessageFolder(newPearlType))
                        {
                            pearlsToPurge.Add(eventName);
                        }
                    }
                }
                foreach (String pearlToPurge in pearlsToPurge)
                {
                    queuedClips.Remove(pearlToPurge);
                    Console.WriteLine("Queue contains a pearl " + pearlToPurge + " which conflicts with " + newPearlType);
                }
            }
            return isValid;
        }

        private Boolean clipIsPearlOfWisdom(String eventName)
        {
            foreach (PearlsOfWisdom.PearlType pearlType in Enum.GetValues(typeof(PearlsOfWisdom.PearlType)))
            {
                if (pearlType != PearlsOfWisdom.PearlType.NONE && PearlsOfWisdom.getMessageFolder(pearlType) == eventName)
                {
                    return true;
                }
            }
            return false;
        }

        private Boolean hasPearlJustBeenPlayed()
        {
            return timeLastPearlOfWisdomPlayed.Add(minTimeBetweenPearlsOfWisdom) > DateTime.UtcNow;
        }
    }
}
