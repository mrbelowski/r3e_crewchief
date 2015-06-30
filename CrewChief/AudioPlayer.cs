using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Media;
using CrewChief.Events;
using System.Windows.Media;

namespace CrewChief
{
    class AudioPlayer
    {
        private Dictionary<String, List<SoundPlayer>> clips = new Dictionary<String, List<SoundPlayer>>();

        private String soundFolderName = Properties.Settings.Default.sound_files_path;

        private String backgroundFolderName = Properties.Settings.Default.background_sound_files_path;

        private float volume = Properties.Settings.Default.background_volume;

        private Random random = new Random();
    
        private Dictionary<String, QueueObject> queuedClips = new Dictionary<String, QueueObject>();

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

        class QueueObject
        {
            public long dueTime;
            public AbstractEvent abstractEvent;
            public QueueObject(long dueTime, AbstractEvent abstractEvent)
            {
                this.abstractEvent = abstractEvent;
                this.dueTime = dueTime;
            }
        }

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
            Console.WriteLine("Backgroun sound dir full path = " + backgroundFilesPath);
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
                                    if (soundFile.Name.EndsWith(".wav"))
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
            queueClip("smoke_test/test", 0, null);
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
            backgroundPlayer = new MediaPlayer();
            backgroundPlayer.MediaEnded += new EventHandler(backgroundPlayer_MediaEnded);
            backgroundPlayer.Volume = volume;
            setBackgroundSound(dtmPitWindowClosedBackground);
            while (true) { 
                Thread.Sleep(1000);
                long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                List<String> keysToRemove = new List<String>();
                List<String> keysToPlay = new List<String>();
                lock (Lock)
                {
                    if (loadNewBackground && backgroundToLoad != null)
                    {
                        Console.WriteLine("Setting background sounds file to  " + backgroundToLoad);
                        String path = Path.Combine(soundFilesPath, backgroundFilesPath, backgroundToLoad);
                        backgroundPlayer.Open(new System.Uri(path, System.UriKind.Absolute));
                        loadNewBackground = false;
                    }
                    
                    if (queuedClips.Count > 0)
                    {
                        Console.WriteLine("There are {0} queued events", queuedClips.Count);
                    }
                    foreach (KeyValuePair<String, QueueObject> entry in queuedClips)
                    {
                        Console.WriteLine("Sound " + entry.Key + " is queued to play after " + entry.Value.dueTime + ". It is now " + milliseconds);
                        if (entry.Value.dueTime <= milliseconds)
                        {
                            if ((entry.Value.abstractEvent == null || entry.Value.abstractEvent.isClipStillValid(entry.Key)) && !keysToPlay.Contains(entry.Key))
                            {
                                keysToPlay.Add(entry.Key);
                            }
                            else
                            {
                                Console.WriteLine("Clip " + entry.Key + " is no longer valid");
                            }
                            keysToRemove.Add(entry.Key);
                        }
                    }
                    if (keysToPlay.Count > 0)
                    {
                        playSounds(keysToPlay);
                    }
                    foreach (String key in keysToRemove)
                    {
                        Console.WriteLine("Removing {0} from queue", key);
                        queuedClips.Remove(key);
                    }
                }
            }
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
    
        // we pass in the event which triggered this clip so that we can query the event before playing the
        // clip to check if it's still valid against the latest game state. This is necessary for clips queued
        // with non-zero delays (e.g. you might have crossed the start / finish line between the clip being 
        // queued and it being played)
        public void queueClip(String eventName, int secondsDelay, AbstractEvent abstractEvent) {
            lock (Lock)
            {
                if (queuedClips.ContainsKey(eventName))
                {
                    Console.WriteLine("Clip for event " + eventName + " is already queued, ignoring");
                    return;
                }
                else
                {
                    Console.WriteLine("Queuing clip for event " + eventName);
                    long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    queuedClips.Add(eventName, new QueueObject(milliseconds + (secondsDelay * 1000), abstractEvent));
                }
            }
        }

        public void removeQueuedClip(String eventName)
        {
            lock (Lock)
            {
                if (queuedClips.ContainsKey(eventName))
                {
                    queuedClips.Remove(eventName);
                }
            }
        }
    
        private void playSounds(List<String> eventNames) {

            Boolean oneOrMoreEventsEnabled = false;
            foreach (String eventName in eventNames) {
                if (enabledSounds.Contains(eventName))
                {
                    oneOrMoreEventsEnabled = true;
                }
            }
            if (oneOrMoreEventsEnabled)
            {
                // this looks like we're doing it the wrong way round but there's a short
                // delay playing the event sound, so if we kick off the background before
                // the beep things sound a bit more natural
                int backgroundDuration = 0;
                int backgroundOffset = 0;
                if (backgroundPlayer.NaturalDuration.HasTimeSpan)
                {
                    backgroundDuration = (backgroundPlayer.NaturalDuration.TimeSpan.Minutes * 60) +
                        backgroundPlayer.NaturalDuration.TimeSpan.Seconds;
                    Console.WriteLine("Duration from file is " + backgroundDuration);
                    backgroundOffset = random.Next(0, backgroundDuration - backgroundLeadout);
                }
                Console.WriteLine("Background offset = " + backgroundOffset);
                backgroundPlayer.Position = TimeSpan.FromSeconds(backgroundOffset);
                backgroundPlayer.Play();
                if (enableStartBleep)
                {
                    List<SoundPlayer> bleeps = clips["start_bleep"];
                    int bleepIndex = random.Next(0, bleeps.Count);
                    bleeps[bleepIndex].PlaySync();
                }
                foreach (String eventName in eventNames)
                {
                    if (enabledSounds.Contains(eventName))
                    {
                        List<SoundPlayer> clipsList = clips[eventName];
                        int index = random.Next(0, clipsList.Count);
                        SoundPlayer clip = clipsList[index];
                        Console.WriteLine("playing the sound at position " + index + ", name = " + clip.SoundLocation);
                        clip.PlaySync();
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
                backgroundPlayer.Stop();
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
    }
}
