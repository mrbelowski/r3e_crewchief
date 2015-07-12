using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChief.Events;

namespace CrewChief
{
    class QueuedMessage
    {
        public static String compoundMessageIdentifier = "COMPOUND_";

        private String folderNameOh = "numbers/oh";
        private String folderNamePoint = "numbers/point";
        private String folderNameStub = "numbers/";
        private String folderZeroZero = "numbers/zerozero";

        // if a queued message is a gap filler, it's only played if the queue only contains 1 other message
        public Boolean gapFiller = false;
        public long dueTime;
        public AbstractEvent abstractEvent;
        public TimeSpan timeSpan;
        public List<String> messagesBeforeTimeSpan = new List<String>();
        public List<String> messagesAfterTimeSpan = new List<String>();
        // todo: check they're valid in the constructors
        public Boolean isValid = true;

        public QueuedMessage(int secondsDelay, AbstractEvent abstractEvent) {
            this.dueTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond + (secondsDelay * 1000);
            this.abstractEvent = abstractEvent;
        }

        // used for creating a pearl of wisdom message where we need to copy the dueTime from the original
        public QueuedMessage(long dueTime, AbstractEvent abstractEvent)
        {
            this.dueTime = dueTime;
            this.abstractEvent = abstractEvent;
        }

        public QueuedMessage(List<String> messages, int secondsDelay, AbstractEvent abstractEvent)
        {
            if (messages != null && messages.Count > 0)
            {
                this.messagesBeforeTimeSpan.AddRange(messages);
            }
            this.dueTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond + (secondsDelay * 1000);
            this.abstractEvent = abstractEvent;
        }

        public QueuedMessage(String messageBeforeTimeSpan, String messageAfterTimeSpan, TimeSpan timeSpan, int secondsDelay, AbstractEvent abstractEvent)
        {
            if (messageBeforeTimeSpan != null)
            {
                this.messagesBeforeTimeSpan.Add(messageBeforeTimeSpan);
            }
            if (messageAfterTimeSpan != null)
            {
                this.messagesAfterTimeSpan.Add(messageAfterTimeSpan);
            }
            this.timeSpan = timeSpan;
            this.dueTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond + (secondsDelay * 1000);
            this.abstractEvent = abstractEvent;
        }

        public QueuedMessage(List<String> messagesBeforeTimeSpan, List<String> messagesAfterTimeSpan, TimeSpan timeSpan, int secondsDelay, AbstractEvent abstractEvent)
        {
            if (messagesBeforeTimeSpan != null && messagesBeforeTimeSpan.Count > 0)
            {
                this.messagesBeforeTimeSpan.AddRange(messagesBeforeTimeSpan);
            }
            if (messagesAfterTimeSpan != null && messagesAfterTimeSpan.Count > 0)
            {
                this.messagesAfterTimeSpan.AddRange(messagesAfterTimeSpan);
            }
            this.timeSpan = timeSpan;
            this.dueTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond + (secondsDelay * 1000);
            this.abstractEvent = abstractEvent;
        }

        public List<String> getMessageFolders()
        {
            List<String> messages = new List<String>();
            messages.AddRange(messagesBeforeTimeSpan);
            if (timeSpan != null)
            {
                messages.AddRange(getTimeMessageFolders(timeSpan));
            }
            messages.AddRange(messagesAfterTimeSpan);
            return messages;
        }

        private List<String> getTimeMessageFolders(TimeSpan timeSpan)
        {
            List<String> messages = new List<String>();
            if (timeSpan != null)
            {
                // if the milliseconds would is > 949, when we turn this into tenths it'll get rounded up to 
                // ten tenths, which we can't have. So move the timespan on so this rounding doesn't happen
                if (timeSpan.Milliseconds > 949)
                {
                    timeSpan = timeSpan.Add(TimeSpan.FromMilliseconds(1000 - timeSpan.Milliseconds));
                }
                if (timeSpan.Minutes > 0)
                {
                    messages.AddRange(getFolderNames(timeSpan.Minutes, ZeroType.NONE));
                    if (timeSpan.Seconds == 0)
                    {
                        // add "zero-zero" for messages with minutes in them
                        messages.Add(folderZeroZero);
                    }
                    else
                    {
                        messages.AddRange(getFolderNames(timeSpan.Seconds, ZeroType.OH));
                    }
                }
                else
                {
                    messages.AddRange(getFolderNames(timeSpan.Seconds, ZeroType.NONE));
                }
                int tenths = (int)Math.Round(((double) timeSpan.Milliseconds / 100));
                messages.Add(folderNamePoint);
                if (tenths == 0)
                {
                    messages.Add(folderNameStub + 0);
                }
                else
                {
                    messages.AddRange(getFolderNames(tenths, ZeroType.NONE));
                }
            }
            return messages;
        }

        private List<String> getFolderNames(int number, ZeroType zeroType)
        {
            List<String> names = new List<String>();
            if (number < 60)
            {
                // only numbers < 60 are supported
                if (number < 10)
                {
                    // if the number is < 10, use the "oh two" files if we've asked for "oh" instead of "zero"
                    if (zeroType == ZeroType.OH)
                    {
                        if (number == 0)
                        {
                            // will this block ever be reached?
                            names.Add(folderNameOh);
                        }
                        else
                        {
                            names.Add(folderNameStub + "0" + number);
                        }
                    }
                    else if (zeroType == ZeroType.ZERO)
                    {
                        names.Add(folderNameStub + 0);
                        if (number > 0)
                        {
                            names.Add(folderNameStub + number);
                        }
                    }
                    else
                    {
                        names.Add(folderNameStub + number);
                    }                   
                }
                else
                {
                    // > 10 so use the actual number
                    names.Add(folderNameStub + number);
                }
            }
            return names;
        }

        private enum ZeroType
        {
            NONE, OH, ZERO
        }
    }
}
