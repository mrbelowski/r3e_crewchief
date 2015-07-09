﻿using System;
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
            if (messagesBeforeTimeSpan != null && messagesBeforeTimeSpan.Count > 0)
            {
                this.messagesBeforeTimeSpan.AddRange(messagesBeforeTimeSpan);
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
                if (timeSpan.Minutes > 0)
                {
                    messages.AddRange(getFolderNames(timeSpan.Minutes, ZeroType.NONE));
                    if (timeSpan.Seconds == 0)
                    {
                        // add "zero-zero" for messages with minutes in them
                        messages.Add(folderNameStub + 0);
                        messages.Add(folderNameStub + 0);
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
                    messages.Add(folderNameStub + tenths);
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
                    if (zeroType == ZeroType.OH)
                    {
                        names.Add(folderNameOh);
                    }
                    else if (zeroType == ZeroType.ZERO)
                    {
                        names.Add(folderNameStub + 0);
                    }
                    if (number > 0)
                    {
                        names.Add(folderNameStub + number);
                    }
                } 
                else if (number <= 20)
                {
                    names.Add(folderNameStub + number);
                }
                else if (number > 20)
                {
                    int tens = ((int)(number / 10));
                    int units = number - (10 * tens);
                    names.Add(folderNameStub + (tens * 10));
                    if (units > 0)
                    {
                        names.Add(folderNameStub + units);
                    }
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