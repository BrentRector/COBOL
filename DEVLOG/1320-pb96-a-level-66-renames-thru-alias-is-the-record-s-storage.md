## Entry 1320 — 2026-08-18 13:16 PDT — PB96: a level-66 RENAMES THRU alias is the record's storage window — a REDEFINES view inside the range no longer counts twice, a redefining operand maps to the area it overlays, a boundary inside a leaf is a partial view

**What landed.** kb/Work **PB96** (MAJOR, wrong answer): `66 AC RENAMES A THRU C.` over `05 A PIC X(2). 05 B
REDEFINES A PIC X(2). 05 C PIC X(2).` read "ababcd" / LENGTH 6 where the record's area is "abcd" / 4 —
`DataBinder.ResolveRedefines`'s RENAMES loop listed every leaf between FROM and THRU in declaration order, a
REDEFINES view included, as if it occupied its own characters. §13.18.45.4 GR2 defines the THRU alias as an
alphanumeric group item over the elementary items from data-name-2 to data-name-3, and §13.18.44 makes a
redefinition ADD NO storage — the alias is the record's STORAGE WINDOW from data-name-2's first character to
data-name-3's last. Now: `RenamesInfo.Span` is a list of `RenamesSpanPart(Leaf, Occurrence, Start, Length)` that
TILES the window `[Offset(FROM), Offset(THRU) + Width(THRU) − 1]` — `Offset` being the ONE recursive storage
function (a redefiner sits at its target's offset; a redefining sibling adds no width) — greedily over EVERY leaf
of the record, REDEFINES views included: a view reads the storage it overlays exactly as the redefined entry does,
and the first cut that excluded views broke NC252A, whose `RDF8-5 THRU RDF8-6` lies inside a double redefinition of
an OCCURS 36 table and is tiled by exactly those two views. At each position the longest whole leaf / whole table
cell that starts there and fits wins, else a partial slice of the most specific cell containing the position; the
cursor advances by what was covered, so nothing is counted twice. A whole part expands every occurrence; an
occurrence part is the cell's place, a partial one its ref-mod view (`RefModPlace(cell, start, length)`);
`RenamesPlace` now carries each part's width, so `WriteRenames` distributes a store by the parts. The alias's
picture is the window's width; a window the leaves cannot tile is COBOLNET1655. The
shorter-redefinition-as-data-name-3 reading (the alias ends at data-name-3's last character — GnuCOBOL's and
IBM's offset arithmetic; GR2's "includes all elementary items … concluding with data-name-3" is silent about an
overlay's extent) is a CONFORMANCE.md §4 determination.

**Evidence.** `2023/pb96_renames_span_over_redefines`: over `A X(4) / B REDEFINES A X(2) / C X(2) / G { G1 X(2), G2
X(2) }` — `A THRU C` = "abcdef" 6, `A THRU B` = "ab" 2 (the partial view), `B THRU C` = "abcdef" 6, `C THRU G1` =
"efgh" 4, `B THRU G` = "abcdefghij" 10; `MOVE "12345678" TO BG` lands in A / C / G1 / G2 as 1234 / 56 / 78 / "  ";
`MOVE "XY" TO AB` writes through B's view (A = "XY34", AC = "XY3456"); NC252A pins the table-inside-redefinition shape. Inventory GR-13.18.45.4-1/2 →
CONFORMS (GAP 3958 → 3956).

**Gate.** Characterization 33/33 · Conformance (filtered: Renames/Redefines/Nist/Corpus/Data/Move/Record)
1474/1474 (incl. NC252A, which the first cut broke) · Unit (filtered) 188/188. Battery #17 owed for the PB96 batch with the next landing.

