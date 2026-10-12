## Entry 458 — COBOL.NET G2/G3a: typed data fields + MOVE / ADD / SUBTRACT / DISPLAY (no byte substrate)

The first real data + verb slice on the greenfield compiler. New `CobolNet.Runtime` library (clean, byte-free):
`CobolString.Store` (alphanumeric width/justify semantics) + `CobolNum.FormatUnsignedDisplay`/`TruncateInt`/
`TruncateDecimal` (PIC truncation + DISPLAY imaging). `RoslynBackend` references it and deploys
`CobolNet.Runtime.dll` next to each compiled program (so `dotnet prog.dll` resolves it).

New binding layer (`CobolNet.Binding`): `PicInfo` (analyze a PICTURE+USAGE → category + numeric profile
digits/scale/sign → the .NET type: `X/A`→`string`, integer `9`→`long`, scaled/signed→`decimal`, COMP-1/2→
`float`/`double`, edited→`string`); `DataItem` (the bound record tree + C#-safe name sanitization via Roslyn
`SyntaxFacts`); `DataBinder` (WORKING-STORAGE parse tree → a level-stack-built `DataItem` forest + a by-name index).

`CSharpEmitter` now: emits a static typed field per elementary WS item initialized from its VALUE (or COBOL
default); and emits `DISPLAY` (literals + fields, numeric via `FormatUnsignedDisplay`), `MOVE` (literal/field →
field, type-dispatched on the target), `ADD …TO/GIVING`, `SUBTRACT …FROM/GIVING` on typed numerics.

Verified end-to-end: a program with `PIC X(10)`/`PIC 9(4)` items, VALUE init, MOVE, ADD-TO, ADD-GIVING →
`NAME=ALICE␣␣␣␣␣` / `COUNT=0005` / `AFTER=0008` / `TOTAL=0018` / `NAME2=BOB␣␣␣␣␣␣␣` — all COBOL-correct, clean
generated C#. NEXT: groups→record struct + OCCURS→arrays (G2 cont); COMPUTE/MULTIPLY/DIVIDE/IF/EVALUATE/PERFORM
(G3); then the control-flow engine (G4) and driving the NIST corpus (G5). ROUNDED/SIZE-ERROR/signed-DISPLAY land
when the arithmetic verbs route through the full ported `CobolNum`.

