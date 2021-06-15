using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class CustomLogger : MonoBehaviour
{
    struct LogEntry
    {
        public DateTime timestamp;
        public int threadId;
        public LogType type;
        public string stackTrace;
        public string message;
    }

    ConcurrentQueue<LogEntry> queue = new ConcurrentQueue<LogEntry>();
    StreamWriter streamWriter;
    readonly StringBuilder stringBuilder = new StringBuilder();
    readonly string[] logTypesUpperCase = Enum.GetNames(typeof(LogType)).Select(s => s.Length < 4 ? s.ToUpper() + "\t" : s.ToUpper()).ToArray();
    const string logsFolder = "Logs";

    void Awake()
    {
#if !UNITY_EDITOR
        enabled = false;
#else
        string filename = $"log_{DateTime.Now:o}.txt";
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            filename = filename.Replace(c.ToString(), "_");
        }
        string path = Path.Combine(Application.persistentDataPath, logsFolder);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        string pathname = Path.Combine(path, filename);
        Debug.Log("Writing logs to: " + pathname);
        streamWriter = new StreamWriter(pathname);
        streamWriter.WriteLine(filename);
        streamWriter.WriteLine();
        Application.logMessageReceivedThreaded += Application_logMessageReceivedThreaded;
#endif
    }

    void OnDisable()
    {
        streamWriter.Close();
    }

    void Application_logMessageReceivedThreaded(string condition, string stackTrace, LogType type)
    {
        queue.Enqueue(new LogEntry
        {
            timestamp = DateTime.Now,
            threadId = System.Threading.Thread.CurrentThread.ManagedThreadId,
            type = type,
            stackTrace = stackTrace,
            message = condition,
        });
    }

    // Update is called once per frame
    void Update()
    {
        while (queue.TryDequeue(out LogEntry entry))
        {
            streamWriter.Write(entry.timestamp.ToString("o"));
            streamWriter.Write("\t");
            streamWriter.Write(entry.threadId);
            streamWriter.Write("\t");
            streamWriter.Write(logTypesUpperCase[(int)entry.type]);
            streamWriter.Write("\t");
            streamWriter.WriteLine(entry.message);
            if ((entry.type != LogType.Log) && (entry.type != LogType.Warning))
            {
                // Skip first line in stack-trace (it's UnityEngine.Debug:Log) and add tab
                var lines = entry.stackTrace.Split('\n');
                for (int i = 1, iMax = lines.Length; i < iMax; ++i)
                {
                    string line = lines[i];
                    if (!string.IsNullOrEmpty(line))
                    {
                        stringBuilder.Append('\t');
                        stringBuilder.Append(line);
                        stringBuilder.Append('\n');
                    }
                }
                streamWriter.Write(stringBuilder);
                stringBuilder.Clear();
            }
        }
        streamWriter.Flush();
    }
}
