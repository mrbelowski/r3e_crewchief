using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using CrewChief.Data;
using CrewChief.Events;
using System.Collections.Generic;


namespace CrewChief
{
    class Sample : IDisposable
    {
        private bool Mapped
        {
            get { return (_file != null && _view != null); }
        }

        private MemoryMappedFile _file;
        private MemoryMappedViewAccessor _view;

        private readonly TimeSpan _timeInterval = TimeSpan.FromMilliseconds(Properties.Settings.Default.update_interval);

        private List<AbstractEvent> eventsList = new List<AbstractEvent>();

        Shared lastState;
        Shared currentState;

        Boolean stateCleared = false;

        double lastGameStateTime = 0;

        public void Dispose()
        {
            _view.Dispose();
            _file.Dispose();
        }

        public void Run()
        {
            var timeReset = DateTime.UtcNow;
            var timeLast = timeReset;
            
            Console.WriteLine("Looking for RRRE.exe...");
            AudioPlayer audioPlayer = new AudioPlayer();
            audioPlayer.initialise();
            
            eventsList.Add(new LapCounter(audioPlayer));
            eventsList.Add(new LapTimes(audioPlayer));
            eventsList.Add(new Penalties(audioPlayer));
            eventsList.Add(new MandatoryPitStops(audioPlayer));            
            eventsList.Add(new Fuel(audioPlayer));
            eventsList.Add(new Position(audioPlayer));
            eventsList.Add(new RaceTime(audioPlayer));
            eventsList.Add(new TyreTempMonitor(audioPlayer));
            eventsList.Add(new EngineMonitor(audioPlayer));
            eventsList.Add(new Timings(audioPlayer));
            eventsList.Add(new DamageReporting(audioPlayer));

            while (true)
            {
                var timeNow = DateTime.UtcNow;

                if (timeNow.Subtract(timeLast) < _timeInterval)
                {
                    Thread.Sleep(1);
                    continue;
                }

                timeLast = timeNow;

                if (Utilities.IsRrreRunning() && !Mapped)
                {
                    Console.WriteLine("Found RRRE.exe, mapping shared memory...");

                    if (Map())
                    {
                        Console.WriteLine("Memory mapped successfully");
                        timeReset = DateTime.UtcNow;
                    }
                }

                if (Mapped)
                {
                    lastState = currentState;
                    currentState = new Shared();
                    _view.Read(0, out currentState);

                    // how long has the game been running?
                    double gameRunningTime = currentState.Player.GameSimulationTime;
                    // if we've gone back in time, this means a new session has started - 
                    // clear all the game state
                    if ((gameRunningTime <= _timeInterval.Seconds || gameRunningTime < lastGameStateTime)
                        && !stateCleared)
                    {
                        Console.WriteLine("Clearing game state...");
                        foreach (AbstractEvent abstractEvent in eventsList)
                        {
                            abstractEvent.clearState();
                        }
                        stateCleared = true;
                    }
                    else if (gameRunningTime > _timeInterval.Seconds) 
                    {
                        stateCleared = false;
                        foreach (AbstractEvent abstractEvent in eventsList)
                        {
                            abstractEvent.trigger(lastState, currentState);
                        }
                    }
                    lastGameStateTime = currentState.Player.GameSimulationTime;
                }
            }
        }

        private bool Map()
        {
            try
            {
                _file = MemoryMappedFile.OpenExisting(Constant.SharedMemoryName);
                _view = _file.CreateViewAccessor(0, Marshal.SizeOf(typeof(Shared)));
                return true;
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }
    }
    class Program
    {
        static void MainSafe(string[] args)
        {
            using (var sample = new Sample())
            {
                sample.Run();
            }
        }

        static void Main(string[] args)
        {
            if (Debugger.IsAttached)
            {
                MainSafe(args);
            }
            else
            {
                try
                {
                    MainSafe(args);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.ToString());
                }
            }
        }
    }
}
