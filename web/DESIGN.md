# HRM Next — Product UI v2

Stitch project: `projects/3656751257367590154`. New visual direction after the user rejected the previous rounded-card concept. Preserve the previous project as history.

## Purpose and visual direction

Vietnamese HR staff need to locate an employee, check their status and update a profile. Make the employee register the dominant visual element. A restrained, light workspace with precise alignment and comfortable data density. Character comes from the employee information hierarchy, useful row selection and orderly work controls, rather than a marketing hero or ornamental widgets.

## Tokens

- White work surface (#FFFFFF): table and main content.
- Cool mist (#F7F8FA): quiet sidebar and table heading.
- Graphite (#20252B): main text.
- Slate (#626B77): helper text.
- Divider grey (#E3E6EB): table row and region boundaries.
- Work blue (#3157D5): a single primary action, active navigation and focus.

Use Be Vietnam Pro with Segoe UI/system fallback. Body 14–16px, title 22px/600, table headers sentence case/13px. No monospace display typography or unnecessary all-caps labels. Functional radius: 4px on inputs/buttons, 6px on drawer, no card shadow on main workspace. Limit long copy; left-align prose and data labels, align numbers consistently.

## Desktop composition

1440px viewport: light 216px sidebar, 56px header, 24px content margins. A single page title “Nhân viên”, short count and one “Thêm nhân viên” action. Tabs for employee status, then local search and department/status filters. One full-width table with 8–10 example rows at approximately 48px each, pagination at the bottom. Metadata is quieter than names. Row hover and selected state use a faint blue wash and a narrow accent. Use initials instead of stock portraits. One compact “Dữ liệu minh họa” note outside the table. No repeated proof badges, department widgets, analytics tiles, online status, global search, branch switching or notification counts.

The edit drawer belongs to a separate interaction state, not a permanent second dashboard alongside an unrelated approval/calendar example. Desktop width about 420px; visible input labels, named save/cancel actions, clear loading/validation/conflict handling. Do not add salary fields to a general personnel editor.

## Mobile and interaction

375px viewport: drawer navigation, 16px padding, 44px touch targets. Filters may open in a compact sheet. Table scroll is contained in its own labeled region; the page itself must not overflow. Prefer a readable employee list when adapting to a narrow view. Visible keyboard focus and text contrast at least 4.5:1. Motion only answers interaction, with reduced-motion support. Do not claim responsive verification from a generated image alone.

## Scope and review

All names, counts, dates and statuses shown are fictional examples. Actual actions/fields will be limited by server permissions. No SSO, biometrics, insurance rates, extra payroll policies, bulk-edit/export guarantees or invented HR features. The web implementation remains separate from this design concept.

Review before generation: the old template used large greeting cards and repeated tags; this design replaces that hierarchy with the employee register. A marketing hero and stock font pairing suggested by the UX catalog were rejected because this is an internal application. Keep only applicable UX guidance, particularly contained mobile tables and accessible feedback. Avoid adding bulk edits just because a catalog recommends them.

## Applied web scope

The current application applies this direction to the shell, authentication pages, account settings and dashboard. Business routes retain clear unavailable states until their API workflows are integrated. Do not add the sample employee register to production as fake data. Shared styling lives in src/styles/product.css; Ant Design tokens are set in src/main.tsx. Auth/session and route authorization behavior remain unchanged.

T17 integrates personnel, own profile, departments/positions, calendar versions, accounts/roles/explicit HR scopes and audit routes with the generated API client. Attendance and payroll workflows remain separate tasks. Personnel filters and pagination run on the server; the employee drawer sends changed fields only and requires explicit comparison/reload after a version conflict. Calendar previews use the backend calculation, with a clear distinction between a proposed full-month preview and the saved effective date. Creation does not imply HR access: the success result supplies the profile identifier for a SystemAdmin to grant explicitly. No example personnel are bundled into production.

T18 integrates attendance and leave: personal and HR workspaces, a scoped attendance register, deliberate correction/OT forms, annual leave balances, an approval queue and a month view for a selected leave range. Keep the same quiet table hierarchy. Use employee labels resolved only for the current page, clear server-state tags, explicit reasons and visible recovery after a conflict. Closed payroll months show read-only actions. Decimal strings lose only insignificant zeros in display; request values retain their precision. Payroll screens remain a separate task.
