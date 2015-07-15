using System;
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
        private Boolean channelOpen = false;

        private Boolean requestChannelOpen = false;
        private Boolean requestChannelClose = false;
        private Boolean holdChannelOpen = false;

        private readonly TimeSpan queueMonitorInterval = TimeSpan.FromMilliseconds(1000);

        private Dictionary<String, List<SoundPlayer>> clips = new Dictionary<String, List<SoundPlayer>>();

        private String soundFolderName = Properties.Settings.Default.sound_files_path;

        private String backgroundFolderName = Properties.Settings.Default.background_sound_files_path;

        private float backgroundVolume = Properties.Settings.Default.background_volume;

        private readonly TimeSpan minTimeBetweenPearlsOfWisdom = TimeSpan.FromSeconds(Properties.Settings.Default.minimum_time_between_pearls_of_wisdom);

        private Boolean sweary = Properties.Settings.Default.use_sweary_messages;

        // if this is true, no 'green green green', 'get ready', or spotter messages are played
        private Boolean disableImmediateMessages = Properties.Settings.Default.disable_immediate_messages;

        private Random random = new Random();
    
        private OrderedDictionary queuedClips = new OrderedDictionary();

        private OrderedDictionary immediateClips = new OrderedDictionary();

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
                ThreadStart work;
                if (disableImmediateMessages)
                {
                    Console.WriteLine("Interupting and immediate messages are disabled - no spotter or 'green green green'");
                    work = monitorQueueNoImmediateMessages;
                } else
                {
                    work = monitorQueue;
                }
                Thread thread = new Thread(work);
                thread.Start();
                new SmokeTest(this).trigger(new Data.Shared(), new Data.Shared());
            }
            catch (DirectoryNotFoundException e) {
                Console.WriteLine("Unable to find sounds directory - path: " + soundFolderName);
            }
        }

        public void setBackgroundSound(String backgroundSoundName)
        {
            backgroundToLoad = backgroundSoundName;
            loadNewBackground = true;
        }

        private void initialiseBackgroundPlayer()
        {
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
        }
        private void monitorQueue() {
            Console.WriteLine("Monitor starting");
            initialiseBackgroundPlayer();
            var timeLast = DateTime.UtcNow;
            while (true)
            {
                if (requestChannelOpen)
                {
                    Console.WriteLine("Requesting channel open");
                    openRadioChannelInternal();
                    requestChannelOpen = false;
                    holdChannelOpen = true;
                }
                if (!holdChannelOpen && channelOpen)
                {
                    Console.WriteLine("Closing open channel");
                    closeRadioInternalChannel();
                }
                if (immediateClips.Count > 0)
                {
                    Console.WriteLine("Playing immediate clips");
                    foreach (String key in immediateClips.Keys)
                    {
                        Console.WriteLine(key + ", ");
                    }
                    playQueueContents(immediateClips, true);
                }
                if (requestChannelClose)
                {
                    Console.WriteLine("Requesting close channel");
                    if (channelOpen && queuedClips.Count == 0)
                    {
                        closeRadioInternalChannel();
                    }
                    requestChannelClose = false;
                    holdChannelOpen = false;
                }
                var timeNow = DateTime.UtcNow;
                if (timeNow.Subtract(timeLast) < queueMonitorInterval)
                {
                    Thread.Sleep(50);
                    continue;
                }
                timeLast = timeNow;
                playQueueContents(queuedClips, false);
            }
        }

        private void monitorQueueNoImmediateMessages() {
            initialiseBackgroundPlayer();
            while (true)
            {
                Thread.Sleep(queueMonitorInterval);
                if (!holdChannelOpen && channelOpen)
                {
                    Console.WriteLine("Closing open channel");
                    closeRadioInternalChannel();
                }
                playQueueContents(queuedClips, false);
            }
        }
        private void playQueueContents(OrderedDictionary queueToPlay, Boolean isImmediateMessages)
        {
            long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            List<String> keysToPlay = new List<String>();
            List<String> soundsProcessed = new List<String>();
            lock (queueToPlay)
            {                
                foreach (String key in queueToPlay.Keys)
                {
                    QueuedMessage queuedMessage = (QueuedMessage)queueToPlay[key];
                    if (queuedMessage.dueTime <= milliseconds)
                    {
                        if ((queuedMessage.abstractEvent == null || queuedMessage.abstractEvent.isClipStillValid(key)) &&
                            !keysToPlay.Contains(key) && (!queuedMessage.gapFiller || playGapFillerMessage()) && 
                            (queuedMessage.expiryTime == 0  || queuedMessage.expiryTime > milliseconds))
                        {
                            keysToPlay.Add(key);
                        }
                        else
                        {
                            Console.WriteLine("Clip " + key + " is not valid");
                            soundsProcessed.Add(key);
                        }
                    }
                }
                if (keysToPlay.Count > 0)
                {
                    Boolean oneOrMoreEventsEnabled = false;
                    if (keysToPlay.Count == 1 && clipIsPearlOfWisdom(keysToPlay[0]) && hasPearlJustBeenPlayed())
                    {
                        Console.WriteLine("Rejecting pearl of wisdom " + keysToPlay[0] +
                            " because one has been played in the last " + minTimeBetweenPearlsOfWisdom + " seconds");
                        soundsProcessed.Add(keysToPlay[0]);
                    }
                    else
                    {
                        foreach (String eventName in keysToPlay)
                        {
                            if ((eventName.StartsWith(QueuedMessage.compoundMessageIdentifier) &&
                                ((QueuedMessage)queuedClips[eventName]).isValid) || enabledSounds.Contains(eventName))
                            {
                                oneOrMoreEventsEnabled = true;
                            }
                        }
                    }                   
                    
                    if (oneOrMoreEventsEnabled)
                    {
                        openRadioChannelInternal();
                        soundsProcessed.AddRange(playSounds(keysToPlay, isImmediateMessages));                        
                    }
                    else
                    {
                        Console.WriteLine("All events are disabled");
                    }
                    // only close the channel if we've processed the entire queue
                    /*Console.WriteLine("Can we close? " + !isImmediateMessages + ", " + soundsProcessed.Count + ", " + keysToPlay.Count);
                    if (!isImmediateMessages && soundsProcessed.Count == keysToPlay.Count && !holdChannelOpen)
                    {
                        closeRadioInternalChannel();
                    }*/
                    Console.WriteLine("finished playing");
                }
                foreach (String key in soundsProcessed)
                {
                    Console.WriteLine("Removing {0} from queue", key);
                    queueToPlay.Remove(key);
                }
            }
        }

        private List<String> playSounds(List<String> eventNames, Boolean isImmediateMessages)
        {
            List<String> soundsProcessed = new List<String>();
            Console.WriteLine("About to process queue contents: " + String.Join(", ", eventNames.ToArray()));

            foreach (String eventName in eventNames)
            {
                // if there's anything in the immediateClips queue, stop processing
                if (isImmediateMessages || immediateClips.Count == 0)
                {
                    if ((eventName.StartsWith(QueuedMessage.compoundMessageIdentifier) &&
                    ((QueuedMessage)queuedClips[eventName]).isValid) || enabledSounds.Contains(eventName))
                    {
                        if (clipIsPearlOfWisdom(eventName))
                        {
                            if (hasPearlJustBeenPlayed())
                            {
                                Console.WriteLine("Rejecting pearl of wisdom " + eventName +
                                    " because one has been played in the last " + minTimeBetweenPearlsOfWisdom + " seconds");
                                soundsProcessed.Add(eventName);
                                continue;
                            }
                            else
                            {
                                timeLastPearlOfWisdomPlayed = DateTime.UtcNow;
                            }
                        }
                        if (eventName.StartsWith(QueuedMessage.compoundMessageIdentifier))
                        {
                            foreach (String message in ((QueuedMessage)queuedClips[eventName]).getMessageFolders())
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
                    soundsProcessed.Add(eventName);
                }
                else
                {
                    Console.WriteLine("we've been interrupted");
                }               
            }
            Console.WriteLine("Processed " + String.Join(", ", soundsProcessed.ToArray()));
            return soundsProcessed;
        }

        private void openRadioChannelInternal()
        {
            Console.WriteLine("Opening channel, current state is " + channelOpen);
            if (!channelOpen)
            {
                channelOpen = true;
                if (backgroundVolume > 0 && loadNewBackground && backgroundToLoad != null)
                {
                    Console.WriteLine("Setting background sounds file to  " + backgroundToLoad);
                    String path = Path.Combine(soundFilesPath, backgroundFilesPath, backgroundToLoad);
                    backgroundPlayer.Open(new System.Uri(path, System.UriKind.Absolute));
                    loadNewBackground = false;
                }

                // this looks like we're doing it the wrong way round but there's a short
                // delay playing the event sound, so if we kick off the background before the bleep
                if (backgroundVolume > 0)
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
                    Console.WriteLine("playing start beep");
                    List<SoundPlayer> bleeps = clips["start_bleep"];
                    int bleepIndex = random.Next(0, bleeps.Count);
                    bleeps[bleepIndex].PlaySync();
                }
            }
            Console.WriteLine("Channel is open");
        }

        private void closeRadioInternalChannel()
        {
            Console.WriteLine("Closing channel, current state is " + channelOpen);
            if (channelOpen)
            {
                if (enableEndBleep)
                {
                    List<SoundPlayer> bleeps = clips["end_bleep"];
                    int bleepIndex = random.Next(0, bleeps.Count);
                    bleeps[bleepIndex].PlaySync();
                }
                if (backgroundVolume > 0)
                {
                    backgroundPlayer.Stop();
                }
                channelOpen = false;
            }
            Console.WriteLine("Channel is closed");
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

        public void openChannel()
        {
            requestChannelOpen = true;
        }

        public void closeChannel()
        {
            requestChannelClose = true;
        }

        public void playClipImmediately(String eventName, QueuedMessage queuedMessage)
        {
            if (disableImmediateMessages)
            {
                return;
            }
            lock (immediateClips)
            {
                if (immediateClips.Contains(eventName))
                {
                    Console.WriteLine("Clip for event " + eventName + " is already queued, ignoring");
                    return;
                }
                else
                {
                    immediateClips.Add(eventName, queuedMessage);
                }
            }
        }

        public void queueClip(String eventName, QueuedMessage queuedMessage,  PearlsOfWisdom.PearlType pearlType, double pearlMessageProbability)
        {
            lock (queuedClips)
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
                        QueuedMessage pearlQueuedMessage = new QueuedMessage(queuedMessage.abstractEvent);
                        pearlQueuedMessage.dueTime = queuedMessage.dueTime;
                        queuedClips.Add(PearlsOfWisdom.getMessageFolder(pearlType), pearlQueuedMessage);
                    }
                    queuedClips.Add(eventName, queuedMessage);
                    if (pearlPosition == PearlsOfWisdom.PearlMessagePosition.AFTER)
                    {
                        QueuedMessage pearlQueuedMessage = new QueuedMessage(queuedMessage.abstractEvent);
                        pearlQueuedMessage.dueTime = queuedMessage.dueTime;
                        queuedClips.Add(PearlsOfWisdom.getMessageFolder(pearlType), pearlQueuedMessage);
                    }
                }
            }
        }

        public void removeQueuedClip(String eventName)
        {
            lock (queuedClips)
            {
                if (queuedClips.Contains(eventName))
                {
                    queuedClips.Remove(eventName);
                }
            }
        }

        public void removeImmediateClip(String eventName)
        {
            if (disableImmediateMessages)
            {
                return;
            }
            lock (immediateClips)
            {
                if (immediateClips.Contains(eventName))
                {
                    immediateClips.Remove(eventName);
                }
            }
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
