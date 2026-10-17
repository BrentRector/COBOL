*>--- conformance-case
*> id: pb358-alternate-key-source-duplicates
*> rules: [SR-14.9.41.3-7]
*> source-format: free
*> portable: unverified
*>---
*> THE CLAUSE IS RECOGNIZED WHOLE, not merely up to the SOURCE token - which is the property the generic
*> parse error could not have, and the reason this fixture is not a duplicate of pb358-alternate-key-source.
*> ISO 1989:2023 12.4.5.6.2's general format continues past the key-form brace group with two bracketed
*> phrases, [ WITH DUPLICATES ] and [ SUPPRESS WHEN literal-1 ] (printed page 350, folio 320, RENDERED).
*> Before 2026-09-09 the parser lost its place at SOURCE and then reported a SECOND unexpected-token error
*> at WITH - two diagnostics for one clause, neither of them naming the actual disposition. With the SOURCE
*> arm parsed, the trailing DUPLICATES phrase is consumed by the same clause and exactly ONE diagnostic is
*> produced: the named Annex A.3 item 40 decline.
*> WITH is an optional word here (un-underlined; only DUPLICATES carries a rule), so this spelling exercises
*> the full phrase. SUPPRESS WHEN is deliberately NOT written: it is a COBOL-2023 addition gated by
*> COBOLNET0900 below 2023 (constructs.json row alternate-key-suppress-when-2023), and a fixture rejecting
*> at all four editions must not depend on which of two codes arrives first.
*> Annex A.3 item 40; docs/CONFORMANCE.md section 2 row 40; 4.2.6 paragraph 3. kb/Work PB358.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB358AKSRCDUP.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT F-IX ASSIGN TO "pb358aksrcdup.dat"
        ORGANIZATION IS INDEXED
        ACCESS MODE IS DYNAMIC
        RECORD KEY IS F-K1
        ALTERNATE RECORD KEY IS F-ALT SOURCE IS F-K2 WITH DUPLICATES.
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
