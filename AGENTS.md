# CampusX coding-agent instructions

## Sources and approval

- `Milestone-0/CampusX_SRS.docx` is the authority for functional and non-functional requirements. Read the relevant sections before changing behavior.
- Approved repository-accessible class specifications govern entities, relationships, and validation subject to the SRS. No such document is currently checked in. The externally reviewed document leaves validation and several fields unspecified. Until an approved copy is available in the repository, do not depend on machine-specific paths or assume teammates have it; request it when needed.
- `Milestone-1/CampusX-community-design-green.pdf` contains Direction 2 foundations and screens; `Milestone-1/campus-green-ui-only.pdf` contains the same screen pages. Use them as visual/interaction references. File names, timestamps, sample content, and mockups do not establish requirements or approval. Do not ship "Specification pending" annotations.
- The approved green CampusX Home UI direction includes branding, centered GET search, sidebar, compact campus hero, responsive product cards, and EGP prices. Preserve these decisions over the older mockup's generic name and unconfirmed currency. The final implemented UI has not been explicitly verified and approved in full; direction approval does not approve every implementation detail, PDF screen, or later change.
- `Milestone-0/CampusX_BRD.docx` provides supporting business context subordinate to the SRS. Its future enhancements are not current scope.
- Report conflicts and missing rules. Ask before resolving a material requirement/class/design conflict; do not silently invent the decision. Preserve implemented UI unless the requested task explicitly changes it.

## Required scope

The current SRS has section numbers and named requirements, but no FR, BR, or NFR identifiers. Do not invent IDs or claim ID-level traceability. Read the source for full requirements; preserve these essentials:

- **Registration and Login; Product Management** (SRS §3): validate student registration/login/logout; students add products with name, description, price, category, and image, manage only their own listings, and mark them sold.
- **Search and Browsing; Product Details; Communication** (§3): browse available products, search/filter by name/category/price, show product fields plus seller information and status, and contact sellers.
- **Admin Management; student profile and reporting** (§§2, 3, 5): students manage profiles and report inappropriate products; admins manage student accounts/products, remove inappropriate content, handle reported products or users, and monitor the platform.
- **Performance, Security, Usability, Reliability, Compatibility, Maintainability** (§4): responsive actions/search; secure password storage, owner-only product management and admin-only administration; simple clear UI; consistent user/product data; common browsers and different screen sizes; understandable, maintainable code. No numeric performance targets or exact browser matrix are specified.

The BRD, not the SRS, supplies these IDs: **BR-01** student access; **BR-02** product listing; **BR-03** discovery; **BR-04** product information; **BR-05** seller communication; **BR-06** owner listing control including sold state; **BR-07** content moderation; **BR-08** student account management. Its business rules require registered student sellers, owner-only editing/deletion, identifying product data, admin moderation/authorization, and sold items excluded from normal availability. Revenue work is explicitly authorized: simulated development payments for promotion credits, configurable promotion settings, featured ordering, and commission calculation/disclosure. Real gateway collection, shipping/delivery, accounting, internal chat, and unrelated merchandise remain out of scope until separately approved. Do not add future enhancements or other features without explicit authorization.

## Implementation and unresolved details

- Before each task, inspect the relevant actual code to establish implementation status: controllers, views, BL classes, ViewModels, mappings, and startup. README and earlier feature-status statements may be stale. Existing code is not evidence of requirement approval; a route, view, or method alone does not prove a complete or tested workflow.
- Verify actual class names and relationships. Do not rename/restructure code to match an unavailable conceptual diagram or add enterprise layers without a specific task and resolved design.
- Email/profile fields, product college/phone fields, stored inquiries, report reason/status, account blocking, upload limits, and validation attributes are implementation choices, not automatically approved SRS rules. Account fields/identifier, password rules, contact channel/public seller data, profile schema, category administration, image requirements, price rules, and report fields/origin/handling need confirmation when required by a task.
- Validation is incomplete. Before changing price behavior, check for the previously observed discrepancy between positive bounded add/edit prices and BL/database checks allowing zero. Do not silently choose a rule. Preserve constraints/data and keep server-side ViewModel, BL, and EF validation consistent with agreed rules.
- Test/default seller assignment, direct controller database writes without ownership checks, and development deletion by ID do not satisfy the SRS's owner/admin authorization requirements. Do not treat seeded users, Development mode, or hidden buttons as authentication. Flag these gaps when relevant and enforce authorization server-side in work that implements those workflows.

## Coding and verification

- Work in `Milestone-3/`: ASP.NET Core MVC, .NET 8, C#, Razor, EF Core 8.0.4, SQL Server. Preserve existing routes, bindings, partial views, namespaces, and naming unless the task requires a change; note the existing `ViewModel`/`ViewModels` namespace variation.
- Keep changes small and suitable for a student project. Follow `Models/` and injected `*BL` conventions, `Data/DbContexts/`, `Data/Configurations/`, `Data/Migrations/`, `Controllers/`, `Views/`, and `wwwroot/`. Do not expand direct database access in controllers or perform unrelated architectural refactors.
- Preserve password hashing, owner/admin checks, foreign keys, unique email, non-negative price, and exactly-one-report-target constraints. Use validated server-side mutation paths and antiforgery protection. Never commit credentials or use a teammate's machine address as a universal SQL Server setting.
- Inspect startup code, connection settings, and pending migrations before running the app; Development startup has applied migrations and seeded data automatically. Do not drop databases, delete migrations, rewrite applied migrations, or erase data to bypass a schema mismatch; report missing-column errors and reconcile existing migrations first.
- For code changes, build `Milestone-3/CampusX.csproj`, test the affected workflow and relevant validation/permission/empty/error states, and check responsive behavior for UI changes. Distinguish build warnings from errors. Stop agent-started app processes after verification so they do not lock `CampusX.exe` during later builds. Documentation-only tasks need document/diff checks, not application startup.
- Preserve unrelated local changes and uploads. Report files changed, checks performed, and unresolved issues. Do not commit, push, or redesign UI without authorization.
