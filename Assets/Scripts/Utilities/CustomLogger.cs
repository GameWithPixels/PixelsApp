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
        public int frame;
        public LogType type;
        public string stackTrace;
        public string message;
    }

    readonly ConcurrentQueue<LogEntry> queue = new ConcurrentQueue<LogEntry>();
    StreamWriter streamWriter;
    readonly StringBuilder stringBuilder = new StringBuilder();
    readonly string[] logTypesUpperCase = Enum.GetNames(typeof(LogType)).Select(s => s.Length < 4 ? s.ToUpper() + "\t" : s.ToUpper()).ToArray();
    const string logsFolder = "Logs";
    const string lineSeparator = "===============================================================================";

    void OnEnable()
    {
        string path = Path.Combine(Application.persistentDataPath, logsFolder);

        // Delete older files, we keep the last 20 ones
        if (Directory.Exists(path))
        {
            foreach (var fileInfo in Directory.GetFiles(path)
                     .Select(f => new FileInfo(f))
                     .OrderByDescending(f => f.LastWriteTimeUtc)
                     .Skip(20))
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
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        string pathname = Path.Combine(path, filename);
        Debug.Log("Writing logs to: " + pathname);
        streamWriter = new StreamWriter(pathname);
        streamWriter.WriteLine($"Log started at {DateTime.Now:o}");
        streamWriter.WriteLine();
        streamWriter.WriteLine(lineSeparator);
        streamWriter.WriteLine();

        streamWriter.WriteLine($"\tdeviceModel: {SystemInfo.deviceModel}");
        streamWriter.WriteLine($"\tdeviceName: {SystemInfo.deviceName}");
        streamWriter.WriteLine($"\tdeviceType: {SystemInfo.deviceType}");

        streamWriter.WriteLine($"\tprocessorType: {SystemInfo.processorType}");
        streamWriter.WriteLine($"\tprocessorFrequency: {SystemInfo.processorFrequency}");
        streamWriter.WriteLine($"\tprocessorCount: {SystemInfo.processorCount}");
        streamWriter.WriteLine($"\tsystemMemorySize: {SystemInfo.systemMemorySize}");

        streamWriter.WriteLine($"\tgraphicsDeviceName: {SystemInfo.graphicsDeviceName}");
        streamWriter.WriteLine($"\tgraphicsDeviceType: {SystemInfo.graphicsDeviceType}");
        streamWriter.WriteLine($"\tgraphicsDeviceVendor: {SystemInfo.graphicsDeviceVendor}");
        streamWriter.WriteLine($"\tgraphicsMemorySize: {SystemInfo.graphicsMemorySize}");

        streamWriter.WriteLine($"\tbatteryLevel: {SystemInfo.batteryLevel}");
        streamWriter.WriteLine($"\tbatteryStatus: {SystemInfo.batteryStatus}");
        
        streamWriter.WriteLine($"\toperatingSystem: {SystemInfo.operatingSystem}");
        streamWriter.WriteLine($"\toperatingSystemFamily: {SystemInfo.operatingSystemFamily}");

        streamWriter.WriteLine($"\tsupportsAudio: {SystemInfo.supportsAudio}");
        streamWriter.WriteLine($"\tsupportsLocationService: {SystemInfo.supportsLocationService}");
        
        streamWriter.WriteLine();
        streamWriter.WriteLine(lineSeparator);
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
        streamWriter.Close();
        streamWriter = null;
    }

    void Application_logMessageReceivedThreaded(string condition, string stackTrace, LogType type)
    {
        queue.Enqueue(new LogEntry
        {
            timestamp = DateTime.Now,
            threadId = System.Threading.Thread.CurrentThread.ManagedThreadId,
            frame = Time.frameCount,
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
