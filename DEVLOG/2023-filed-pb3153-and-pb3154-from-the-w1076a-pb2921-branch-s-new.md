## Entry 2023 — 2026-10-14 03:36 PDT — Filed PB3153 and PB3154 from the w1076a (PB2921) branch's new defects

PB3153 records the residue PB3132 named when it was retired: a contained program that uses an inherited SPECIAL-NAMES name (the repro uses a class-name in `IF X IS DIGS`) while declaring a paragraph of the same spelling is not refused, though the flat twin is, because the class-condition, mnemonic, alphabet and symbolic-character lookups record nothing in the one-type-per-word census (ISO 8.3.2.2). The repro and result are the w1076a implementer's, from its build.

PB3154 records an over-refusal that predates PB2921: a FACTORY data item spelling a class named in the class's REPOSITORY, which the factory never uses, is refused COBOLNET2692, because `OoDriver.BindClassData` declares the repository entries again in the factory and instance censuses. Notes only; no code changed.

