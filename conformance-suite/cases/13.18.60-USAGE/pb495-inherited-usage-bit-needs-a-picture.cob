*>--- conformance-case
*> id: pb495-inherited-usage-bit-needs-a-picture
*> rules: [SR-13.18.60.3-5]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB495 — ISO 13.18.60.3 SR5: "An elementary data item with usage bit shall be specified only with a
*> picture character-string that describes a boolean data item." A has usage bit -- 13.18.60.4 GR1 gives it
*> the clause written on G -- and no picture character-string at all, so it satisfies no reading of SR5; and
*> usage bit is NOT among 13.16.3 SR8's picture-less usages, so a PICTURE is required rather than forbidden.
*> The written-clause spelling `05 A USAGE BIT.` has always been refused by name (COBOLNET0881, "an
*> elementary item with USAGE BIT requires a PICTURE clause"); the INHERITED spelling compiled clean and
*> produced a zero-length item that was dropped from the emitted record struct -- the fourth cell of the
*> 2x2 (acquisition arm x {non-boolean picture, no picture}) that the other three already covered.
*> USAGE BIT and category boolean are COBOL-2002 additions (COBOLNET0900 at --std 85).
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB495C.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 G USAGE BIT.
   05 A.
PROCEDURE DIVISION.
    DISPLAY "X".
    STOP RUN.
