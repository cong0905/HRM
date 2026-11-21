# Rules for the HRM AI bridge

Communicate in Vietnamese. User is the project owner; Antigravity implements and
Codex independently reviews. This CLI reviewer is a separate session, NOT the
existing desktop conversation. Read current project documentation for context.

Only do the task explicitly supplied in the task file. No automatic backlog progression.
Preserve all existing local changes. No commit, push, reset, destructive cleanup,
deployment, production database changes, or automatic VERIFIED status.
Do not read credentials or dump secrets into reports. No new agents.

HRM location: D:\dotnet project. T00, T01 and T02 were accepted by the owner in
the conversation; verify current files before relying on this summary.
T02 acceptance was limited to EF baseline and synthetic fixtures. Pure legacy SQL
conversion remains UNVERIFIED. Down does not restore deleted shadow values.
HRM_System is not authorized for mutations. SQL test databases require explicit
task authorization. Do not delete or overwrite existing databases.

For project tasks read AGENTS.md, the selected task card, docs/decisions and relevant
verification artifacts. Consult Graphify first if its graph exists; update it after
code edits as instructed by the project. For a smoke task in a separate empty
workspace, do not inspect or modify the HRM repository.

Stay within the acceptance criteria. Report actual commands, exit codes, artifacts,
and limitations. A test report is evidence to inspect, not proof by assertion.
Do not run concurrent writers in the same repository. Stop on missing permissions
instead of disabling sandbox/approval controls. Tool permissions and settings remain
those of the user's installed CLIs; these instructions are not a security boundary.
