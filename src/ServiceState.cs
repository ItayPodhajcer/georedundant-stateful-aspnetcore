using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DotNext.Net.Cluster.Consensus.Raft;
using DotNext.IO;
using System.Text;

public interface IServiceState
{
  Task SetStateAsync(int id, string State);
  ValueTask<string> GetStateAsync(int id);
}

internal class ServiceState : PersistentState, IServiceState
{
  private record LedgerStateEntry(int Id, string State);

  private ConcurrentDictionary<int, string> _stateLedger = new();

  public ServiceState()
    : base(Path.Combine(AppContext.BaseDirectory, Environment.ProcessId.ToString()), 50)
  { }

  public async Task SetStateAsync(int id, string state)
  {
    var entry = CreateJsonLogEntry(new LedgerStateEntry(id, state));

    var commitIndex = await AppendAsync(entry);
    
    await WaitForCommitAsync(commitIndex, Timeout.InfiniteTimeSpan, CancellationToken.None);
  }

  public ValueTask<string> GetStateAsync(int id) =>
    ValueTask
      .FromResult(_stateLedger
        .TryGetValue(id, out string state)
          ? state
          : default);

  protected override ValueTask ApplyAsync(LogEntry entry)
      => entry.Length == 0L ? new ValueTask() : UpdateValue(entry);
  
  private async ValueTask UpdateValue(LogEntry entry)
  {
    var jsonString = await entry.ToStringAsync(Encoding.UTF8);
    var value = (LedgerStateEntry) await entry
      .DeserializeFromJsonAsync(_ => typeof(LedgerStateEntry))
      .ConfigureAwait(false);

    _stateLedger.AddOrUpdate(value.Id, value.State, (_, _) => value.State);
  }
}