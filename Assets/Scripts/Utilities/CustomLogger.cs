using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class CustomLogger : SingletonMonoBehaviour<CustomLogger>
{
    struct LogEntry
    {
        public DateTime timestamp;
        public int threadId;
        public int frame;
        public LogType type;
        public string stackTrace;
        public string message;
    }

    readonly ConcurrentQueue<LogEntry> queue = new ConcurrentQueue<LogEntry>();
    FileStream fileStream;
    StreamWriter streamWriter;
    readonly StringBuilder stringBuilder = new StringBuilder();
    readonly string[] logTypesUpperCase = Enum.GetNames(typeof(LogType)).Select(s => s.Length < 4 ? s.ToUpper() + "\t" : s.ToUpper()).ToArray();
    const string logsFolder = "Logs";
    const string lineSeparator = "===============================================================================";

    public static string LogsDirectory => Path.Combine(Application.persistentDataPath, logsFolder);

    public string LogPathname { get; private set; }

    public bool IsFileOpened => streamWriter != null;

    public void Suspend(Action<string> action)
    {
        try
        {
            CloseStream();
            Debug.Log($"{nameof(CustomLogger)} suspended");
            action(LogPathname);
        }
        finally
        {
            Debug.Log($"{nameof(CustomLogger)} restored");
            OpenStream();
        }
    }

    void OpenStream()
    {
        if (!IsFileOpened)
        {
            // Open file with shared access so others can read it
            fileStream = File.Open(LogPathname, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            streamWriter = new StreamWriter(fileStream);
        }
    }

    void CloseStream()
    {
        streamWriter?.Close();
        streamWriter = null;
        fileStream?.Close();
        fileStream = null;
    }

    void OnEnable()
    {
        // Delete older files, we keep the most 20 files counting the new one we are about to create
        if (Directory.Exists(LogsDirectory))
        {
            foreach (var fileInfo in Directory.GetFiles(LogsDirectory)
                     .Select(f => new FileInfo(f))
                     .OrderByDescending(f => f.LastWriteTimeUtc)
                     .Skip(19))
            {
                fileInfo.Delete();
            }
        }

        // Create new log file
        string now = $"{DateTime.Now:o}";
        string filename = $"log_{now}.txt";
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            filename = filename.Replace(c.ToString(), "_");
        }
        if (!Directory.Exists(LogsDirectory))
        {
            Directory.CreateDirectory(LogsDirectory);
        }

        LogPathname = Path.Combine(LogsDirectory, filename);
        Debug.Log("Writing logs to: " + LogPathname);

        OpenStream();

        streamWriter.WriteLine($"Log started at {DateTime.Now:o}");
        streamWriter.WriteLine();
        streamWriter.WriteLine(lineSeparator);
        streamWriter.WriteLine();

        streamWriter.WriteLine($"deviceModel: {SystemInfo.deviceModel}");
        streamWriter.WriteLine($"deviceName: {SystemInfo.deviceName}");
        streamWriter.WriteLine($"deviceType: {SystemInfo.deviceType}");

        streamWriter.WriteLine($"processorType: {SystemInfo.processorType}");
        streamWriter.WriteLine($"processorFrequency: {SystemInfo.processorFrequency}");
        streamWriter.WriteLine($"processorCount: {SystemInfo.processorCount}");
        streamWriter.WriteLine($"systemMemorySize: {SystemInfo.systemMemorySize}");

        streamWriter.WriteLine($"graphicsDeviceName: {SystemInfo.graphicsDeviceName}");
        streamWriter.WriteLine($"graphicsDeviceType: {SystemInfo.graphicsDeviceType}");
        streamWriter.WriteLine($"graphicsDeviceVendor: {SystemInfo.graphicsDeviceVendor}");
        streamWriter.WriteLine($"graphicsMemorySize: {SystemInfo.graphicsMemorySize}");

        streamWriter.WriteLine($"batteryLevel: {SystemInfo.batteryLevel}");
        streamWriter.WriteLine($"batteryStatus: {SystemInfo.batteryStatus}");

        streamWriter.WriteLine($"operatingSystem: {SystemInfo.operatingSystem}");
        streamWriter.WriteLine($"operatingSystemFamily: {SystemInfo.operatingSystemFamily}");

        streamWriter.WriteLine($"supportsAudio: {SystemInfo.supportsAudio}");
        streamWriter.WriteLine($"supportsLocationService: {SystemInfo.supportsLocationService}");

        streamWriter.WriteLine();
        streamWriter.WriteLine(lineSeparator);
        streamWriter.WriteLine();
        streamWriter.WriteLine("Format: timestamp, thread id, rendering frame, severity, message");
        streamWriter.WriteLine();

        Application.logMessageReceivedThreaded += Application_logMessageReceivedThreaded;
    }

    void OnDisable()
    {
        Application.logMessageReceivedThreaded -= Application_logMessageReceivedThreaded;
        streamWriter.WriteLine();
        streamWriter.WriteLine(lineSeparator);
        streamWriter.WriteLine();
        streamWriter.WriteLine($"Log ended at {DateTime.Now:o}");

        CloseStream();
    }

    void Application_logMessageReceivedThreaded(string condition, string stackTrace, LogType type)
    {
        int threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        queue.Enqueue(new LogEntry
        {
            timestamp = DateTime.Now,
            threadId = threadId,
            frame = threadId == 1 ? Time.frameCount : -1,
            type = type,
            stackTrace = stackTrace,
            message = condition,
        });
    }

    // Update is called once per frame
    void Update()
    {
        if (IsFileOpened)
        {
            while (queue.TryDequeue(out LogEntry entry))
            {
                streamWriter.Write(entry.timestamp.ToString("o"));
                streamWriter.Write("\t");
                streamWriter.Write(entry.threadId);
                if (entry.threadId < 1000) streamWriter.Write("\t");
                streamWriter.Write("\t");
                streamWriter.Write(entry.frame);
                if (entry.frame < 1000) streamWriter.Write("\t");
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
}
