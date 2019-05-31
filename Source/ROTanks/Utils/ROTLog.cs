using System;
using UnityEngine;

namespace ROTanks
{
    public static class ROTLog
    {
        public static readonly bool debugMode = true;

        public static void stacktrace()
        {
            MonoBehaviour.print(System.Environment.StackTrace);
        }

        public static void log(string line)
        {
            MonoBehaviour.print("ROT-LOG  : " + line);
        }

        public static void log(System.Object obj)
        {
            MonoBehaviour.print("ROT-LOG  : " + obj);
        }

        public static void error(string line)
        {
            MonoBehaviour.print("ROT-ERROR: " + line);
        }

        public static void debug(string line)
        {
            if (!debugMode) { return; }
            MonoBehaviour.print("ROT-DEBUG: " + line);
        }

        public static void debug(object obj)
        {
            if (!debugMode) { return; }
            MonoBehaviour.print(obj);
        }

    }
}
