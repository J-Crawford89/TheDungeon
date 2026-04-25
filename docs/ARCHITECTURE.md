# TheDungeon — architecture conventions

This document codifies how the **C# / Godot** codebase is structured and how we extend it. It complements `.cursor/rules/architecture.mdc` (same rules, optimized for tooling).

## Assembly tiers and references

Projects under `Scripts/` are **layered**. **A project must not reference a project in a higher tier.**

| Tier | Project | May reference |
|------|---------|----------------|
| Foundation | `0.Core` (`TheDungeon.Core`) | *(none of the other script projects)* |
| Parallel on Core | `2.State`, `3.Game.Contracts` | `0.Core` only |
| Game rules | `3.Game` (`TheDungeon.Game`) | `0.Core`, `2.State`, `3.Game.Contracts` |
| Presentation | `4.UI` | `0.Core`, `2.State`, `3.Game`, `3.Game.Contracts` |

**`3.Game.Contracts`** is the home for **DTOs, result types, and shared request/response shapes** used across Game and UI. It must stay **thin**: no references upward, and no game-rule implementation—only types that describe data crossing boundaries.

**Composition root:** Code that talks to Godot resources and the scene tree (e.g. `Scenes/GameRoot.cs`, `Repositories/Godot*Repository.cs`) lives **outside** this strict stack but **wires** implementations into interfaces declared in lower tiers. Services depend on **abstractions** (e.g. `I*Repository` in Core), not on Godot-specific types.

## Domain models and DTOs

- **Domain types** (in Core) and **DTOs** (especially in Contracts) are **dumb containers**: properties, nested types, maybe simple enums.
- **Do not** embed game rules, validation orchestration, or multi-step workflows inside these types.
- **Behavior** belongs in **services**, **state mutation** code, and **helpers**. Prefer making invalid states unrepresentable via **construction and APIs** at those layers rather than “smart” domain objects.

## Dependency injection vs statics

- **Prefer explicit injection:** pass dependencies via constructors or small context objects (e.g. `GameRunContext`) assembled once per run.
- Avoid **static mutable** service state and **service locator** patterns for core game logic.
- **`static`** is appropriate for **pure functions**, constants, and small **stateless helpers**—not as a default way to reach services.

## UI reactivity and events

As features multiply, **many services** can affect what the UI should show. Prefer:

1. **Events / notifications** raised when meaningful game state changes (combat resolved, inventory changed, floor revealed, etc.).
2. **UI/presenter layers** that **subscribe** and map those signals to controls.

Reserve **direct** calls like `RefreshX()` from deep services into specific panels for **simple** flows or transitional code; new work should **bias toward** listener-style updates.

## C# object construction

- **Default style:** types that need several field/property assignments should use a **parameterless constructor** plus **object initializer**:

  ```csharp
  var x = new MyRecord
  {
      A = a,
      B = b,
  };
  ```

- **When to use non-default constructors:** unavoidable invariants, **immutable** aggregates, **Godot** lifecycle and `[Export]` usage, performance-critical allocations, or consistency with an existing family of types that already uses rich constructors.

---

For Godot-specific editing constraints (scenes vs scripts), see `.cursor/rules/godot-no-tscn-edits.mdc`.

Concrete decisions and policy lock-ins are tracked in `docs/ARCHITECTURE_DECISIONS.md`.
