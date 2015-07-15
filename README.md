# r3e_crewchief

Version 1.1



Quick start...
--------------

You need to install .net 4 or above to use the app. Download the CrewChief.zip file, extract it somewhere (anywhere, the app's not fussy), and run the enclosed CrewChief.exe. Then fire up R3E and be amazed at my poor voice acting.

If you want to debug or modify the app, you'll need VS2013.

There's no volume control in the app - you'll have to tweak the volume in RaceRoom and the volume of CrewChief in the Windows sounds mixer to get levels you like.

The relative volume of the background sounds can be controlled by modifying the background_volume property in CrewChief.exe.config - the background_volume can be set between 0 (off) and 1 (max).

The update frequency can also be set here using the update_interval property (in milliseconds). It's currently updating every 0.1 seconds.

For version 1.2, the tyre temp thresholds are a bit of a guess. In DTM 2014, WTCC 2014 and M1 Procar racing I'm seeing temperatures in the 90s (i assume unit is Celsius) when driving normally. 100+ happens if I arse about doing doughnuts and stuff, so I set the 'hot' threshold to 100, but this might need some tuning. If you don't want tyre messages to trigger you can set hot and cold thresholds to something very high so the app always thinks the tyres are cold (there are no messages for cold tyres).

For version 1.3 I've added engine temperature monitoring, but there's an issue in the game with this. The reported in-game temps are very low, like the radiator is far too effective. I've used unrealistic warning thresholds just so the warning messages will trigger occasionally.

For version 1.4 there are now semi-random pearls of wisdom / helpful advice / annoying patronising nonsense added to some messages. Hopefully these aren't intrusive or too regular (or too embarrassing)

For version 1.6 there are lots of new things but it's mostly filling in the blanks now the memory block is more complete. There are also optional 'sweary' messages - nothing too excessive (just stuff like "aww shit, we just had a penalty"). Set the CrewChief.exe.config "use_sweary_messages" to False if you don't like them

For version 1.8 I've added compound messages to assemble lap times and stuff. Doing this always sounds robotic and my attempt is no different...

For version 1.9 there's a crude spotter (it doesn't now which side the overlapping car is on). This is enabled by default but it's still being tweaked. If it annoys or misleads it can be turned off in the Crewchief.exe.config - enable_spotter.

Changes
-------

Version 1.9.1: Added crude spotter and reworked queuing, interrupting, and 'immediate play' code to support this; added some more fuel half distance checks and race start time checks, added messages for p11 - p24 and messages for last and consistently last; reworked gap / laptime reporting and made the number reading a bit less robotic; some other minor fixes and tweaks.

Version 1.8.5: fixed damage reporting for races where damage is disabled *again*...; delay damage messages a bit

Version 1.8.4: fixed damage reporting for races where damage is disabled; don't report lap time in-race when a lap is invalid

Version 1.8.3: Added some rudimentary damage reporting

Version 1.8.2: Fixed bugs in gap reporting, fuel calculation and pit stop window times for online races

Version 1.8.1: shortened number sounds a bit to make them slightly less hesitant; fixed a rounding issue where it might read out "point ten" for the tenths

Version 1.8: Fixed some lap time reporting issues - the new version of R3E inserts -1 into the PreviousLapTime variable if you go off track (it used to just record the lap time). The app now takes account of this; Make 'this is your last lap' message trigger only when crossing the start line for timed races and only when you're leading (insufficient data in the block to support this event if you're not leading); Added lap time reporting to qual sessions; added position reporting to qual sessions; added gap (front and behind) reporting to race sessions.

Version 1.7: Fixed race end logic so the messages only fire when you cross the line; reduced queue check frequency to prevent multiple queued messages being broken up (extra beeps in between); added last lap events to timed races

Version 1.6: The version includes lots of stuff that wasn't previously possible, now S3 have added more things to the shared memory block. There's still more to come (like monitoring the gaps and relative laptimes, damage monitoring, tyre wear and pressure monitoring etc).

Added pit window events for timed races - this also includes an estimate of when you need to pit so you will get a 'pit on this lap' message if it's near the end of the pit window, and a 'box now' message when you hit sector 3 on that lap; Added tyre-specific pit messages for DTM 2014 ('change to primes'); Revised race time stuff so it now tells you how long you have left in the race (if time left <= 20 mins); Revised penalties so they now include what type the penalty is; Added time penalty message and 'box now' message; Added 'green green green' to start and race finished events to all race types; Don't add pearls of wisdom to non-race sessions for some events; Reworked fuel use monitor; Reworked engine temps monitor so it now compares temps to baseline temps established early in the race (race only); Various other fixes and, no doubt, new bugs

Version 1.5: Don't spam 'session time' events when joining an online practice / qual part way through; fixed stupid wrong calculation of pit window close lap

Version 1.4: Fixed NPE in engine monitor when starting app after starting game; Lowered tyre temp 'hot' temp to trigger more warnings; Added pearls of wisdom. This adds some encouragement to some events. There are 'good', 'neutral' and 'must do better' messages. They are triggered by a probability (i.e. if you take the lead there's a 50% chance of a 'good' message). The messages sometimes play before the event they're associated with, sometimes after. There will only ever be 1 such message queued at a time and you won't get more than one every 40 seconds (so you won't hear a 'good' then shortly after a 'must do better' event). The min time between pearls is in the config file, and they can also be disabled from there.

Version 1.3: Fixed null pointer exception in tyre temp monitor - this happens when you start a race and then start this app; added engine temperature monitoring (note the caveat above)

Version 1.2: Dropped background volume a bit; Added tyre temperature monitoring. This looks that the average tyre temp (across the tread) over the course of a lap. If a tyre's average temperature is above some threshold, an event is triggered ("Your left front is hot", "your rear tyres are hot" etc). It doesn't keep spamming this message - it only informs of changes (i.e. if the next lap your temps are back to normal you'll get a "tyre temps look good" message, or if they're all all you'll get a "all your tyres are hot").

Version 1.1: Updated the update frequency to 10 times per second (this is now an user configurable property). Probably overkill but it might resolve some funkiness with race-finish messages (thanks to Georg for the bug report); 

Version 1.0: We'll call this the first version even though it clearly isn't. This version includes lots of events along with filtered and processed sound files, background sound support (mixed in with the event sounds in real time), and lots of other awesome stuff. This version has an issue with race finish messages in DTM 2014. One user (we'll call him Georg, because that's his name) wins pretty much all the races he runs (grrrr) but sometimes the app doesn't congratulate him (poor fella). Because I mostly lose I don't get to test the 'wayhay, you won' message much, but it's *always* worked for me on those rare and magical occasions when I don't suck. Sometimes Georg gets a 'hard luck' message reserved for slow and hopeless losers like myself, even when he won. This might be down to the relatively low update frequency (once every 2 seconds) in this version. Or the code: if (driverName=='Georg') { alien = true; alwaysBeMeanToHim = true;}.


Some background...
------------------

This is a simple pit - car radio app which takes its data from the shared memory block that Race Room exposes. The goal of this app is primarily to allow players to race without the need for the game's in car 'overlays'. These overlays show stuff like lap times, penalties, tyre change warnings etc. They're a bit too big, intrusive and immersion breaking for my liking but they do contain essential information. This app allows you to turn them off and still get the essential information in the form of radio messages. Hopefully this also adds to the immersion and fun of an already great game.

At some point in the future the game's developer (S3S) will probably add their own pit radio, making this app redundant. There's also the possibility that they change their shared data in such a way as to stop this app working. Having said that, S3S have been (and continue to be) very supportive and helpful so I think this app should be useful for a while to come.

The app is written in C# and uses a substantial chunk of code S3S provided to poll and hold that shared data. I've added event handling, audio file playing / queuing and some other bits and bobs. C# is not my main language - in fact this is the first thing I've ever written in a Microsoft technology (I'm an old Java developer) so I've been feeling my way around. The code quality and project structure reflect this lack of experience.

The sound files are recorded using a Shure SM58 mic into Audacity at 22050Hz (16 bit PCM wav). I've compressed, clipped, normalised, and filtered them to make them more radio-y but they're not great. Neither is my acting. The background files are recorded from the game while sitting in the pits with the engine sound off as cars whizz by.

You can do what you like with all the code and sound files. I assert no ownership or copyright on any of it.

Limitations...

The share memory block wasn't designed with this in mind. There's enough data in there to do a lot of useful pit radio messages but there's also a lot missing. The app works best with the DTM 2014 experience because this has mandatory pit stops and a race length of a fixed number of laps. Races with a time length aren't properly supported (the time remaining isn't in the data block). I'll add support as S3S add data to the shared memory block.

Future plans...

The plan for this app has changed a bit since it was started. I'll continue to fix bugs here and there but dev effort is now being focused on a more powerful and flexible Lua version. When S3 change the content of the shared memory block it's quite likely that those changes will *not* be accommodated in this C# version of the app.

It's possible to replace the sound files with whatever you want, but I won't add alternative sound packs here unless the author is happy with my care-free attitude to ownership.
