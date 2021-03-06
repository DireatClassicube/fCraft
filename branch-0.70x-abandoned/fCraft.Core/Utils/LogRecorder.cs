﻿// Part of fCraft | Copyright (c) 2009-2012 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt
using System;
using System.Collections.Generic;
using System.Threading;
using fCraft.Events;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> A simple way to temporarily hook into fCraft's Logger.
    /// Make sure to dispose this class when you are done recording.
    /// The easiest way to ensure that is with a using(){...} block. </summary>
    public sealed class LogRecorder : IDisposable {
        readonly object locker = new object();
        readonly List<string> messages = new List<string>();
        readonly LogType[] thingsToLog;
        bool disposed;
        readonly Thread creatingThread;


        /// <summary> Creates a recorder for errors and warnings. </summary>
        public LogRecorder()
            : this( true, LogType.Error, LogType.Warning ) {
        }


        /// <summary> Creates a custom recorder. </summary>
        /// <param name="restrictToThisThread"> Whether this log recorder should limit
        /// recording to messages emitted from the same thread that created this object. </param>
        /// <param name="thingsToLog"> A list or array of LogTypes to record. </param>
        public LogRecorder( bool restrictToThisThread, [NotNull] params LogType[] thingsToLog ) {
            if( thingsToLog == null ) throw new ArgumentNullException( "thingsToLog" );
            Logger.Logged += HandleLog;
            this.thingsToLog = thingsToLog;
            if( restrictToThisThread ) {
                creatingThread = Thread.CurrentThread;
            }
        }


        void HandleLog( object sender, LogEventArgs e ) {
            if( creatingThread != null && creatingThread != Thread.CurrentThread ) return;
            for( int i = 0; i < thingsToLog.Length; i++ ) {
                if( thingsToLog[i] != e.MessageType ) continue;
                lock( locker ) {
                    messages.Add( e.MessageType + ": " + e.RawMessage );
                    switch( e.MessageType ) {
                        case LogType.SeriousError:
                        case LogType.Error:
                            HasErrors = true;
                            break;
                        case LogType.Warning:
                            HasWarnings = true;
                            break;
                    }
                }
            }
        }


        /// <summary> Whether any messages have been recorded. </summary>
        public bool HasMessages {
            get { return messages.Count > 0; }
        }

        /// <summary> Whether any errors have been recorded. </summary>
        public bool HasErrors { get; private set; }

        /// <summary> Whether any errors have been recorded. </summary>
        public bool HasWarnings { get; private set; }


        /// <summary> An array of individual recorded messages. </summary>
        public string[] MessageList {
            get {
                lock( locker ) {
                    return messages.ToArray();
                }
            }
        }

        
        /// <summary> All messages in one block of text, separated by newlines. </summary>
        public string MessageString {
            get {
                lock( locker ) {
                    return String.Join( Environment.NewLine, messages.ToArray() );
                }
            }
        }


        /// <summary> Stops recording the messages (cannot be resumed).
        /// This method should be called when you are done with the object.
        /// If LogRecorder is in a using() block, this will be done for you. </summary>
        public void Dispose() {
            lock( locker ) {
                if( !disposed ) {
                    Logger.Logged -= HandleLog;
                    disposed = true;
                }
            }
        }


        /// <summary> Clears all messages recorded at this point. </summary>
        public void ClearMessages() {
            lock( locker ) {
                messages.Clear();
                HasErrors = false;
                HasWarnings = false;
            }
        }
    }
}
