# OpenCode .NET Host

A small .NET 10 console app that hosts a coding agent instead of leaving it in a terminal. It starts a private OpenCode server, creates a session that can only read files and list them by pattern, runs one repository review, and writes an event log.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)
[![Stars](https://img.shields.io/github/stars/mikhailpetrusheuski/opencode-dotnet-host?style=social)](https://github.com/mikhailpetrusheuski/opencode-dotnet-host/stargazers)

Read the write-up: [OpenCode's .NET SDK Hit 1.0 - Your C# App Can Now Own the Agent](https://medium.com/@mikhail.petrusheuski/opencodes-net-sdk-hit-1-0-your-c-app-can-now-own-the-agent-d07207b6995d).

It uses [OpenCodeDotNet.Sdk](https://www.nuget.org/packages/OpenCodeDotNet.Sdk), an unofficial community SDK that is not affiliated with or endorsed by the OpenCode team.

## What it shows

- **Owning the process:** `OpenCodeServer.StartAsync` starts a private server, and the SDK stops it on dispose.
- **Narrowing capabilities:** session rules deny every action, then allow `read` and `glob`. Reading `.env`, `.pem` and `.key` files stays denied. `grep` stays denied too, because its rule checks the search pattern and not the file path, so allowing it could expose files that `read` rules protect. OpenCode's base policy allows every action unless you add rules, so a line in the prompt isn't enough.
- **Keeping evidence:** the live event stream has no replay, so the app writes its own log to `<session id>.log`.
- **Keeping sessions out of domain code:** `IRepositoryReviewer` shows the kind of boundary the rest of an application should depend on. This sample defines it but doesn't implement it.

## Requirements

- .NET 10 SDK
- OpenCode CLI, pinned to the release the SDK's README installs:

  ```bash
  npm install -g @opencode/cli@2.2.5
  ```

- A model provider configured in OpenCode

## Run

Use a throwaway repository first:

```bash
dotnet run -- /path/to/repository
```

The review prints to the console, and the log is written to the current directory. The run has a 10-minute timeout.

## Status

This sample builds against `OpenCodeDotNet.Sdk` 1.0.0, but it hasn't been run against a live OpenCode server yet. Before trusting the rule list, confirm that a forbidden edit is really refused. The rules are a starting point and depend on the job: with `shell` denied, for example, the agent can't run your tests. The name patterns only cover common secret files, so don't point the sample at a repository whose other files you wouldn't send to a model provider.

## Untrusted repositories

Use this sample only on repositories you trust. A repository can carry its own OpenCode configuration, and this sample doesn't establish whether that configuration, including plugins or MCP servers, loads for a session pointed at the repository. To review untrusted code, run the whole host in a disposable sandbox or container with no real credentials.

## License

[MIT](LICENSE)
