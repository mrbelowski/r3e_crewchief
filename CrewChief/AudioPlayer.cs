using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Media;
using CrewChief.Events;

namespace CrewChief
{
    class AudioPlayer
    {
        private Dictionary<String, List<SoundPlayer>> clips = new Dictionary<String, List<SoundPlayer>>();

        // defaults to /sounds in the root folder of the application. If running in debug mode this will have to be
        // a different path
        private String soundFolderName = Properties.Settings.Default.sound_files_path;
        // for debug, something like..
        // private String soundFolderName = "C:/projects/crewchief_c_sharp/CrewChief/CrewChief/sounds";

        private Random random = new Random();
    
        private Dictionary<String, QueueObject> queuedClips = new Dictionary<String, QueueObject>();

        static object Lock = new object();

        List<String> enabledSounds = new List<String>();

        Boolean enableBeep = false;

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
            Console.WriteLine("Sound dir = " + soundFolderName);
            try
            {
                DirectoryInfo soundDirectory = new DirectoryInfo(soundFolderName);
                FileInfo[] bleepFiles = soundDirectory.GetFiles();
                foreach (FileInfo bleepFile in bleepFiles)
                {
                    if (bleepFile.Name.EndsWith(".wav"))
                    {
                        enableBeep = true;
                        openAndCacheClip("bleep", bleepFile.FullName);
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
                // now queue the smoke test
                queueClip("smoke_test/test", 0, new SmokeTest(this));

                // spawn a Thread to monitor the queue
                ThreadStart work = monitorQueue;
                Thread thread = new Thread(work);
                thread.Start();
            }
            catch (DirectoryNotFoundException e) {
                Console.WriteLine("Unable to find sounds directory - path: " + soundFolderName);
            }
        }

        private void monitorQueue() {
            Console.WriteLine("Monitor starting");
            while (true) { 
                Thread.Sleep(1000);
                long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                List<String> keysToRemove = new List<String>();
                List<String> keysToPlay = new List<String>();
                lock (Lock)
                {
                    if (queuedClips.Count > 0)
                    {
                        Console.WriteLine("There are {0} queued events", queuedClips.Count);
                    }
                    foreach (KeyValuePair<String, QueueObject> entry in queuedClips)
                    {
                        Console.WriteLine("Sound " + entry.Key + " is queued to play after " + entry.Value.dueTime + ". It is now " + milliseconds);
                        if (entry.Value.dueTime <= milliseconds)
                        {
                            if (entry.Value.abstractEvent.isClipStillValid(entry.Key) && !keysToPlay.Contains(entry.Key))
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
                SoundPlayer bleep = null;
                if (enableBeep)
                {
                    List<SoundPlayer> bleeps = clips["bleep"];
                    int bleepIndex = random.Next(0, bleeps.Count);
                    bleep = bleeps[bleepIndex];
                    bleep.PlaySync();
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
                if (enableBeep && bleep != null)
                {
                    bleep.PlaySync();
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
    }
}
