LLM Client Tools

Summary
- Utility tools used by DevGPT for interacting with external CLIs and web resources.

Claude CLI Tool
- Tool name: `claude_cli`
- Description: Calls the local Anthropic Claude CLI and returns combined stdout/stderr.
- Requirements:
  - The `claude` CLI must be installed and available on your PATH, and you must be logged in.
  - On Windows, the tool invokes `cmd.exe /c claude ...` so npm global shims (claude.cmd) work.
  - Optional: Set `CLAUDE_CLI_PATH` to the full path of the CLI if it is not on PATH. On Windows, a `.cmd`/`.bat` path is supported.

Environment Override
- `CLAUDE_CLI_PATH`: Full path to the Claude CLI executable. Examples:
  - Windows (npm global shim): `C:\\Users\\you\\AppData\\Roaming\\npm\\claude.cmd`
  - Windows (exe): `C:\\Tools\\claude\\claude.exe`
  - macOS/Linux: `/usr/local/bin/claude`

Timeouts
- You can pass an optional `timeout` (seconds) parameter when invoking the tool. Defaults to 120 seconds.

