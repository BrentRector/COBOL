*>--- conformance-case
*> id: pb358-record-key-source
*> rules: [SR-12.4.5.6.3-6, GR-12.4.5.6.4-2, SR-12.4.5.12.3-5,
*>    GR-12.4.5.12.4-2, SR-14.9.30.3-11, SR-14.9.30.3-12,
*>    SR-14.9.41.3-7, GR-14.9.41.4-15, GR-14.9.41.4-16]
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 Annex A.3 item 40) "The capability of specifying the SOURCE phrase of the RECORD KEY
*> clause and ALTERNATE RECORD KEY clause is dependent on the capabilities of the processor." This
*> implementation does not provide it (docs/CONFORMANCE.md section 2 row 40), and 4.2.6 paragraph 3 makes
*> naming the decline an obligation: "An implementation shall provide a warning mechanism at compile time
*> to indicate use of syntactically-detectable processor-dependent language elements not supported by that
*> implementation."
*> WHY REFUSED, NOT ACCEPTED INERT (COBOLNET1778's RECORD DELIMITER disposition, on the same annex, is the
*> other one): 12.4.5.12.4 GR2 - "Record-key-name-1 defines a record key consisting of the concatenation of
*> all occurrences of data-name-2 in the order specified." record-key-name-1 is its own name class
*> (8.3.2.2.24) and names NO data item, so an inert compile would leave this file with no prime key at all.
*> 4.2.6 paragraph 3's closing sentence is the licence: "The implementor is not required to produce
*> executable code when unsupported processor-dependent language elements are used."
*> The general format was RENDERED from printed page 359 (folio 329), not read off the OCR: RECORD and
*> SOURCE carry underline rules, KEY and both occurrences of IS do not, and the inner { data-name-2 } brace
*> pair takes the ellipsis - so this spelling, with two data-name-2 operands, is the printed form.
*> Before 2026-09-09 the phrase matched no grammar alternative at all and the compiler answered
*> "COBOL0312: unexpected 'SOURCE' ... A period may be missing at the end of the previous sentence" -
*> a generic parse error naming a token and suggesting the wrong repair. kb/Work PB358 / PB293.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB358RKSRC.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F-IX ASSIGN TO "pb358rksrc.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC
        RECORD KEY IS F-COMPOSITE SOURCE IS F-K1 F-K2.
DATA DIVISION.
FILE SECTION.
FD F-IX.
01 F-REC.
   05 F-K1 PIC X(2).
   05 F-K2 PIC X(2).
   05 F-D  PIC X(6).
PROCEDURE DIVISION.
MAIN.
    OPEN INPUT F-IX
    CLOSE F-IX
    STOP RUN.
