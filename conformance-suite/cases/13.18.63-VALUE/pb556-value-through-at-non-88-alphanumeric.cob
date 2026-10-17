*>--- conformance-case
*> id: pb556-value-through-at-non-88-alphanumeric
*> rules: [SR-13.18.63.3-33]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 13.18.63.3 SR33 - the SILENT twin of pb556-value-through-at-non-88, and the reason that one
*> is not enough: the same rule, the same screen, but an ALPHANUMERIC subject, where the glued text is a legal
*> C# string and nothing downstream objects.
*> MEASURED BEFORE the screen, at --std 2023 on this tree: the program compiled CLEAN and X held the three
*> characters `"A` - a quote, an A and a quote, i.e. the source text of `"A" THRU "C"` truncated to the
*> picture width. (The adjudication measured the same at 85, 2002 and 2014; the binder path carries no version
*> predicate either, which is why the rejection band below is all four editions.) A test that only pinned the
*> crashing numeric twin would have left this one passing.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB556THRU05.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 G.
   05 X PIC XXX VALUE "A" THRU "C".
PROCEDURE DIVISION.
MAIN.
    DISPLAY X
    STOP RUN.
