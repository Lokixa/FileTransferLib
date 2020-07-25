using System;
using System.IO;

namespace FtLib
{
    public class Logger
    {
        public enum State
        {
            Silent,
            Progress,
            Debug
        }
        public State CurrentState { get; set; }
        public TextWriter WriteTo;
        public Logger(State state)
        {
            CurrentState = state;
            WriteTo = Console.Out;
        }
        public void Log(string message, State state)
        {
            if (state != CurrentState || state == State.Silent)
            {
                return;
            }
            WriteTo.WriteLine(message);
        }
    }
}