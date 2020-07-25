using System;
using System.IO;

namespace FtLib
{
    public class Logger
    {
        ///<summary>
        /// Different states of the logger.
        ///</summary>
        public enum State
        {
            Silent,
            Progress,
            Debug
        }
        ///<summary>Current state of the logger</summary>
        public State CurrentState { get; set; }
        ///<summary>TextWriter to write to. Default is Console.Out(stdout).</summary>
        public TextWriter WriteTo;
        ///<summary>Sets logger state, and by default writes to Console.Out(stdout).</summary>
        public Logger(State state)
        {
            CurrentState = state;
            WriteTo = Console.Out;
        }
        ///<summary>
        /// Logs message if the state provided matches its current state.
        ///</summary>
        public void Log(string message, State state)
        {
            if (state != CurrentState || state == State.Silent)
            {
                return;
            }
            WriteTo.WriteLine(message);
        }
        ///<summary>
        /// Draws a 2 line progress bar using the function provided.
        /// Expects values that sum to 100.
        ///</summary>
        ///<param name="getNumber">
        /// Func delegate should give numbers that sum to 100,
        /// else the method will misbehave.
        ///</param>
        public static void ProgressBar(Func<int> getNumber)
        {
            Console.WriteLine("\n");
            Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop - 2);
            for (int i = 1; i < 100;)
            {
                i += getNumber();
                // Cap the number to 100
                if (i > 100)
                    i += 100 - i;

                // Draw stars.
                while (Console.CursorLeft < (int)Math.Floor(i / 5.0))           // On every fifth 
                    Console.Write('*');
                int left = Console.CursorLeft;

                // Go to second line and clear.
                Console.SetCursorPosition(0, Console.CursorTop + 1);
                Console.Write(new string(' ', 10));

                // Write progress.
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write($"{i} / 100");

                // Go to first line and sleep.
                Console.SetCursorPosition(left, Console.CursorTop - 1);
                System.Threading.Thread.Sleep(100);
            }
            Console.SetCursorPosition(Console.CursorLeft, Console.CursorTop + 1);
        }
    }
}