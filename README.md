# r3e_crewchief

Quick start...

You need to install .net 4 or above to use the app. Download the CrewChief.zip file, extract it somewhere, and run the enclosed CrewChief.exe. Then fire up R3E and be amazed at my poor voice acting.

If you want to debug or modify the app, you'll need VS2013.

Some introduction...

This is a simple pit - car radio app which takes its data from the shared memory block that Race Room exposes. The goal of this app is primarily to allow players to race without the need for the game's in car 'overlays'. These overlays show stuff like lap times, penalties, tyre change warnings etc. They're a bit too big, intrusive and immersion breaking for my liking but they do contain essential information. This app allows you to turn them off and still get the essential information in the form of radio messages. Hopefully this also adds to the immersion and fun of an already great game.

At some point in the future the game's developer (S3S) will probably add their own pit radio, making this app redundant. There's also the possiblity that they change their shared data in such a way as to stop this app working. Having said that, S3S have been (and continue to be) very supportive and helpful so I think this app should be useful for a while to come.

The app is written in C# and uses a substantial chunk of code S3S provided to poll and hold that shared data. I've added event handling, audio file playing / queuing and some other bits and bobs. C# is not my main language - in fact this is the first thing I've ever written in a Microsoft technology (I'm an old Java developer) so I've been feeling my way around. The code quality and project structure reflect this lack of experience.

The sound files are recorded using a Shure SM58 mic (hence the exessive bass) into Audacity at 22050Hz (16 bit PCM wav). I've compressed, clipped, and normalised them to make them more radio-y but they're not great. Neither is my acting.

You can do what you like with all the code and sound files. I assert no ownership or copyright on any of it.

Limitations...

The share memory block wasn't designed with this in mind. There's enough data in there to do a lot of useful pit radio messages but there's also a lot missing. The app works best with the DTM 2014 experience because this has mandatory pit stops and a race length of a fixed number of laps. Races with a time length aren't properly supported (the time remaining isn't in the data block). I'll add support as S3S add data to the shared memory block.

Future plans...

S3S are adding more data to the shared memory block and as they do I'll extend the app to add more events and make the existing events smarter and support more car classes and race types. I'll also tidy things as I get more confident with the technology. I'll also add a sane build system.

It's possible to replace the sound files with whatever you want, but I won't add alternative sound packs here unless the author is happy with my care-free attitude to ownership.
