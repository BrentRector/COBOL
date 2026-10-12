## Entry 1792 — 2026-09-29 18:51 PDT — PB1141: MERGE closes its USING files after the output procedure

`EmitMerge` ran the whole implicit USING transfer — OPEN, READ loop, CLOSE and the CLOSE USE hook — for every file before entering the output procedure, so inside the procedure each file's FILE STATUS held the CLOSE's `00` instead of the as-if READ's `10`, and a CLOSE declarative ran too early. §14.9.24.4 GR7 c) defers the as-if CLOSE until "after control passes the last statement in the output procedure". `EmitInputFile` is split into the READ half and `EmitInputClose`; SORT keeps its back-to-back pair (its release phase ends with each CLOSE), MERGE emits the closes after the procedure when one is specified. Closes GR-14.9.24.4-9; GAP 801 to 800.

