---
title: Architecture — Map of Content
area: architecture
status: draft
last_updated: 2026-07-23
tags:
  - cobolsharp
  - moc
  - architecture
---

# 🗺 Architecture — Map of Content

What the compiler *is*: its locked invariants and its assembly/module topology.

## Notes in this domain

- [[kb/Architecture/High-Level Design]] — the North Star, the four owner-locked invariants, bound-tree-only pipeline, the `ICodeGenBackend` dual backend, `Place` + PC dispatcher, strict layering, the G0–G8 build order.
- [[kb/Architecture/Module Overview]] — the 5 greenfield assemblies + source-gen; the dependency graph; the bind→emit boundary rule.

## See also

- [[kb/Compiler/MOC]] — the pipeline living inside these assemblies.
- [[kb/IR/MOC]] — the bound tree at the center of the design.
- [[kb/Runtime/MOC]] — the runtime kernel assembly.
- [[kb/Context/Doctrine & Anti-Patterns]] — the layering rules enforced here.
- [[kb/Index]] — knowledge-base home.
