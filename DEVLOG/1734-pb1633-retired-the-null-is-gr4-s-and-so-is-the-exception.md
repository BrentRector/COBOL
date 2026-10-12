## Entry 1734 — 2026-09-27 05:06 PDT — PB1633 retired (the NULL is GR4's, and so is the exception); PB1638 filed for the vendor SET … TO ENTRY spelling

PB1633 claimed that `SET PP TO ADDRESS OF PROGRAM "n"` for a program CONTAINED in the setting program wrongly yields
NULL. §8.4.3.13.4 GR2 gives the address of the OUTERMOST program an externalized name identifies. GR4: a program
that cannot be located gives NULL and sets EC-PROGRAM-NOT-FOUND. Probed with the EC checking on: the
`USE AFTER EXCEPTION CONDITION EC-PROGRAM-NOT-FOUND` declarative runs, `FUNCTION EXCEPTION-STATUS` names the EC, and
the unresumed fatal EC then terminates the run unit (§14.6.13.1.3). The original repro saw a bare NULL only because
checking was off. Retired: not a defect. The note's second question moved to **PB1638 (new)**: the Micro Focus / IBM
spelling `SET … TO ENTRY`, which ISO 2023 does not have, compiles clean at `--std 2023` strict, and nothing
dialect-gates it. The first step is to derive the vendor-extension policy (PB549's reasons, §4.2 flagging) before
anything reaches the owner.

