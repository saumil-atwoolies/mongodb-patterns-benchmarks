using MongoDB.Bson;
using MongoDB.Driver;
using MongoDbPatterns.Benchmarks.Results;

namespace MongoDbPatterns.Benchmarks.Scenarios;

public sealed class ServerStatsCollector
{
    private readonly IMongoDatabase _database;
    private readonly TimeSpan _pollInterval;
    private readonly List<double> _residentMemorySamples = [];
    private CancellationTokenSource? _cts;
    private Task? _pollTask;
    private long _startUserTimeMicros;
    private long _startSystemTimeMicros;
    private DateTime _startWallClock;

    public ServerStatsCollector(IMongoDatabase database, TimeSpan? pollInterval = null)
    {
        _database = database;
        _pollInterval = pollInterval ?? TimeSpan.FromMilliseconds(500);
    }

    public async Task StartAsync(CancellationToken ct)
    {
        var initialStatus = await GetServerStatusAsync(ct);
        _startUserTimeMicros = GetLongSafe(initialStatus, "extra_info", "user_time_us");
        _startSystemTimeMicros = GetLongSafe(initialStatus, "extra_info", "system_time_us");
        _startWallClock = DateTime.UtcNow;

        _residentMemorySamples.Clear();
        RecordMemorySample(initialStatus);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _pollTask = PollAsync(_cts.Token);
    }

    public async Task<ServerStatsResult> StopAsync()
    {
        var stopWallClock = DateTime.UtcNow;

        if (_cts != null)
        {
            await _cts.CancelAsync();
        }

        if (_pollTask != null)
        {
            try { await _pollTask; }
            catch (OperationCanceledException) { }
        }

        // Take a final snapshot for CPU delta
        using var finalCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var finalStatus = await GetServerStatusAsync(finalCts.Token);
        RecordMemorySample(finalStatus);

        var endUserTimeMicros = GetLongSafe(finalStatus, "extra_info", "user_time_us");
        var endSystemTimeMicros = GetLongSafe(finalStatus, "extra_info", "system_time_us");

        var cpuDeltaMicros = (endUserTimeMicros - _startUserTimeMicros)
                           + (endSystemTimeMicros - _startSystemTimeMicros);
        var wallClockMicros = (stopWallClock - _startWallClock).TotalMicroseconds;

        var avgCpuPercent = wallClockMicros > 0
            ? (cpuDeltaMicros / wallClockMicros) * 100.0
            : 0.0;

        var avgMemory = _residentMemorySamples.Count > 0
            ? _residentMemorySamples.Average()
            : 0.0;

        var peakMemory = _residentMemorySamples.Count > 0
            ? _residentMemorySamples.Max()
            : 0.0;

        return new ServerStatsResult
        {
            AverageResidentMemoryMb = Math.Round(avgMemory, 1),
            PeakResidentMemoryMb = Math.Round(peakMemory, 1),
            AverageCpuPercent = Math.Round(avgCpuPercent, 1),
            SampleCount = _residentMemorySamples.Count
        };
    }

    private async Task PollAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_pollInterval, ct);
                var status = await GetServerStatusAsync(ct);
                RecordMemorySample(status);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task<BsonDocument> GetServerStatusAsync(CancellationToken ct)
    {
        var command = new BsonDocument("serverStatus", 1);
        return await _database.RunCommandAsync<BsonDocument>(command, cancellationToken: ct);
    }

    private void RecordMemorySample(BsonDocument serverStatus)
    {
        if (serverStatus.TryGetValue("mem", out var memValue) && memValue is BsonDocument mem
            && mem.TryGetValue("resident", out var resident))
        {
            _residentMemorySamples.Add(resident.ToDouble());
        }
    }

    private static long GetLongSafe(BsonDocument doc, string section, string field)
    {
        if (doc.TryGetValue(section, out var sectionValue) && sectionValue is BsonDocument sectionDoc
            && sectionDoc.TryGetValue(field, out var fieldValue))
        {
            return fieldValue.ToInt64();
        }

        return 0;
    }
}
