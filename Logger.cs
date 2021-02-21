using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Modding
{
    public class Logger
    {
        private static readonly object Locker = new();
        private static readonly StreamWriter Writer;

        internal static readonly Logger API = new Logger("API");

        public readonly string LogName;

        static Logger()
        {
            FileStream fileStream = new FileStream(Path.Combine(Application.persistentDataPath, "ModLog1028.txt"),
                FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            Writer = new StreamWriter(fileStream, Encoding.UTF8) { AutoFlush = true };
        }

        public Logger(string name)
        {
            LogName = name ?? throw new ArgumentNullException();
        }

        public void Log(string message)
        {
            message ??= "null";
            foreach (string line in message.Split('\n'))
            {
                WriteToFile($"[{LogName}] {line}");
            }
        }

        public void Log(object message)
            => Log(message?.ToString());

        private static void WriteToFile(string text)
        {
            lock (Locker)
            {
                Writer.WriteLine(text);
            }
        }
    }
}
