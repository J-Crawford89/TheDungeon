# TheDungeon — Architecture Decisions

This file records concrete, agreed decisions so implementation stays consistent over time.

## ADR-0001: Godot serialized files are editor-owned

- **Status:** Accepted
- **Decision:** Do not edit `*.tscn`, `*.tres`, or other Godot serialized resources in code changes.
- **Rationale:** Text edits to serialized Godot files are brittle and can break scene/resource wiring.
- **Implementation rule:** C# scripts may be changed; scene/resource wiring changes are provided as explicit Godot Editor steps and applied in the editor.

## ADR-0002: UI architecture standard is coordinator/presenter

- **Status:** Accepted
- **Decision:** Keep scene scripts thin; move orchestration and heavy interaction logic into coordinator/presenter classes.
- **Rationale:** Improves testability, reduces scene script sprawl, and avoids competing UI patterns.
- **Scope notes:**
  - `MainUi -> GameUiCoordinator -> presenters` is the preferred model.
  - `NotebookOverlay` heavy logic should be progressively extracted into a coordinator/presenter class rather than expanded inline.

## ADR-0003: Repository duplicate-id policy

- **Status:** Accepted
- **Decision:** For definition repositories, duplicate IDs are handled as **warn + last-wins**.
- **Rationale:** Keeps startup resilient while still surfacing content issues.
- **Implementation rule:** Map input definitions, group by ID, warn for groups with duplicates, keep the last entry per ID.

## ADR-0004: Repository lookup policy (`TryGetById`)

- **Status:** Accepted
- **Decision:** `TryGetById` returns `null` quietly for unknown IDs (no per-lookup warning by default).
- **Rationale:** Avoids noisy logs in normal control flow where misses may be expected.
- **Future note:** When debug diagnostics logging is expanded, optional centralized debug-level lookup tracing may be added.

## ADR-0005: Missing-database fallback policy

- **Status:** Accepted
- **Decision:** Remove implicit fallback default data for missing databases now that prototyping has passed.
- **Rationale:** Missing content should be visible and explicit, not silently replaced.
- **Exception:** Explicit/manual gameplay bootstrap data can exist where intentionally designed and documented.

## ADR-0006: State object helper boundary

- **Status:** Accepted
- **Decision:** State objects may include lightweight helpers and direct data manipulation.
- **Rationale:** Small, local helpers improve ergonomics without forcing unnecessary service indirection.
- **Boundary:** Cross-system orchestration and heavier rule logic remain in service/helper layers.

## ADR-0007: Armor damage-reduction targeting and lookup

- **Status:** Accepted
- **Decision:**
  - `DamageReductionEffectDefinition` targeting precedence is:
    1. `DamageType` set -> applies only to that damage type.
    2. `DamageType` unset + `DamageFamily` set -> applies to that family.
    3. both unset -> applies to all damage.
  - Aggregation stays on `PlayerState` as data fields; lookup logic is handled by `PlayerDamageReductionHelper`.
- **Rationale:** Keeps serialized effect data compact and unambiguous, avoids duplicate armor DR storage, and keeps query logic out of state shape while remaining reusable beyond combat.

