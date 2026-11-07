using System.Text;
using OpenCode.Sdk;
using OpenCode.Sdk.Models;

var repository = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();
using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(10));

await using var server = await OpenCodeServer.StartAsync(
    cancellationToken: timeout.Token);
using var client = server.CreateClient();

var created = await client.Sessions.CreateSessionAsync(
    new SessionCreateRequest
    {
        Title = "architecture review",
        Location = new LocationPublicRef { Directory = repository },
        Permissions = new List<PermissionRule>
        {
            // The last matching rule wins, so the broad deny comes first.
            new() { Action = "*", Resource = "*", Effect = PermissionEffect.Deny },
            new() { Action = "read", Resource = "*", Effect = PermissionEffect.Allow },
            new() { Action = "glob", Resource = "*", Effect = PermissionEffect.Allow },
            // grep stays denied: its rule checks the search pattern, not the file path.
            new() { Action = "read", Resource = "*.env", Effect = PermissionEffect.Deny },
            new() { Action = "read", Resource = "*.env.*", Effect = PermissionEffect.Deny },
            new() { Action = "read", Resource = "*.pem", Effect = PermissionEffect.Deny },
            new() { Action = "read", Resource = "*.key", Effect = PermissionEffect.Deny },
        },
    },
    cancellationToken: timeout.Token);

await using var audit = new StreamWriter($"{created.Session.Id}.log");
var report = new StringBuilder();

// The bus has no replay, so subscribe before prompting.
await using var events = client.Events
    .SubscribeAsync(cancellationToken: timeout.Token)
    .GetAsyncEnumerator(timeout.Token);
await events.MoveNextAsync();

await client.Sessions.GetSessionClient(created.Session.Id).PromptAsync(
    new SessionPromptRequest
    {
        Text = """
            Find the three highest-risk architectural problems in this repository.
            For each one, name the supporting files and one small, reversible fix.
            """,
    },
    cancellationToken: timeout.Token);

while (await events.MoveNextAsync())
{
    if (events.Current is SessionTextDelta delta
        && delta.Data.SessionId == created.Session.Id)
    {
        report.Append(delta.Data.Delta);
        continue;
    }

    await audit.WriteLineAsync($"{DateTimeOffset.UtcNow:O} {events.Current.Type}");

    switch (events.Current)
    {
        case SessionExecutionSucceeded done
            when done.Data.SessionId == created.Session.Id:
            Console.WriteLine(report);
            return;
        case SessionExecutionFailed failed
            when failed.Data.SessionId == created.Session.Id:
            throw new InvalidOperationException(failed.Data.Error.Message);
        case SessionExecutionInterrupted stopped
            when stopped.Data.SessionId == created.Session.Id:
            throw new OperationCanceledException($"Interrupted: {stopped.Data.Reason}");
    }
}
