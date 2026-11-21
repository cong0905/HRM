Connection smoke task, restricted to the supplied empty smoke workspace.
Create exactly one file bridge-smoke.txt containing exactly HRM_BRIDGE_OK followed
by a newline. Read it back and report verification. Do not run any project tasks,
connect databases, or access D:\dotnet project. Reviewer: verify this file's bytes
using read-only access; accept if exact. No other agent-authored artifacts are required.
CLI-generated logs/ directory is runtime housekeeping and is excluded from this criterion.
Antigravity: use native file editing and viewing tools, not terminal commands.
For this smoke test skip any global workflow requiring shell commands or creating logs.
The no-shell restriction above applies ONLY to Antigravity, not the Codex reviewer.
Codex reviewer: you MAY and SHOULD use a read-only shell command to read exact file
bytes and list workspace files. Do not edit, use Git/Graphify, or access HRM.
Coordinator: planning does not need tools for this task.
