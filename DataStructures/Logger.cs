using System;
using System.IO;

namespace FtLib
{
    public enum LoggerState
    {
        Silent = 0b0001,
        Progress = 0b0010,
        Debug = 0b0100,
        Simple = 0b1000,
        All = Simple | Progress | Debug
    }
    public class Logger
    {
        ///<summary>
        /// Different states of the logger.
        ///</summary>
        ///<summary>Current state of the logger</summary>
        public LoggerState CurrentState { get; set; }
        ///<summary>TextWriter to write to. Default is Console.Out(stdout).</summary>
        public TextWriter WriteTo { get; set; }

        ///<summary>Sets logger state, and by default writes to Console.Out(stdout).</summary>
        public Logger(LoggerState state)
        {
            CurrentState = state;
            WriteTo = Console.Out;
        }
        ///<summary>
        /// Logs message if the state provided matches its current state.
        ///</summary>
        public void Log(string message, LoggerState state)
        {
            if ((state & CurrentState) != CurrentState || state == LoggerState.Silent)
            {
                return;
            }
            WriteTo.WriteLine(message);
        }
    }
}