*>--- conformance-case
*> id: national_intrinsics
*> rules: [GR-15.2-3, AR-15.3-9, RV-15.26.4-1, RV-15.26.4-4, RV-15.66.4-1, RV-15.66.4-4, FMT-15.26.2, DOC-A.1-33]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §15.26 DISPLAY-OF / §15.66 NATIONAL-OF — the sanctioned national<->alphanumeric
      *> repertoire conversions (COBOL-2002 intrinsics; P10 national wave; PB59 identity landing).
      *> The implementor-defined correspondence is the Annex A.1 item-33 TOTAL UTF-16 IDENTITY
      *> (CONFORMANCE.md section 7): both repertoires are UTF-16 one code unit per position (item 188),
      *> so ANUM->NAT (§15.66.4 r1) and NAT->ANUM (§15.26.4 r1) both always correspond. No character
      *> lacks a correspondent, so the argument-2 substitution (§15.26.4 r2) and the implementor
      *> substitution + EC-DATA-CONVERSION (§15.26.4 r3) are vacuous BY DECLARATION — the SUB legs
      *> below prove the correspondence is total AND that argument-2 is accepted-and-inert. FUNCTION
      *> LENGTH over both results proves the §15.26.4 r4 / §15.66.4 r4 lengths (character positions).
      *> The wide character below is U+4E16 (CJK, caseless) — the source file and the expected
      *> output are UTF-8 (the standard display device writes UTF-8, CONFORMANCE.md item 59).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NATINTRP10N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N-SRC    PIC N(5) VALUE N"HELLO".
       01 N-WIDE   PIC N(3).
       01 N-DEST   PIC N(4).
       01 N-CMP    PIC N(2).
       01 A-SRC    PIC X(3) VALUE "XYZ".
       01 A-DEST   PIC X(6).
       01 LEN-R    PIC 9(2).
       PROCEDURE DIVISION.
       MAIN.
      *> DISPLAY-OF format 1: national item -> alphanumeric (§15.26.4 r1, identity repertoire).
           MOVE FUNCTION DISPLAY-OF(N-SRC) TO A-DEST.
           DISPLAY "DOF=" A-DEST.
      *> NATIONAL-OF format 1: alphanumeric item -> national (§15.66.4 r1, widening).
           MOVE FUNCTION NATIONAL-OF(A-SRC) TO N-DEST.
           DISPLAY "NOF=" N-DEST.
      *> Round trip over a literal, nested calls (§8.4.3.2 — a function is an argument shape).
           MOVE FUNCTION DISPLAY-OF(FUNCTION NATIONAL-OF("AB")) TO A-DEST.
           DISPLAY "RT=" A-DEST.
      *> The NATIONAL-OF result IS category national (§15.66.1): compares equal to an N"" literal.
           MOVE FUNCTION NATIONAL-OF("AB") TO N-CMP.
           IF N-CMP = N"AB" THEN DISPLAY "EQ=YES" ELSE DISPLAY "EQ=NO".
      *> A wide national character (U+4E16): under the item-33 total identity it HAS an
      *> alphanumeric correspondent (itself).
           MOVE N"A世B" TO N-WIDE.
      *> Argument-2 unspecified: r3 is vacuous under the total correspondence - identity, no EC.
           MOVE FUNCTION DISPLAY-OF(N-WIDE) TO A-DEST.
           DISPLAY "SUB1=" A-DEST.
      *> Argument-2 specified: accepted and inert - r2 has no character to substitute for.
           MOVE FUNCTION DISPLAY-OF(N-WIDE, "#") TO A-DEST.
           DISPLAY "SUB2=" A-DEST.
      *> Result lengths in character positions (§15.50; §15.26.4 r4 / §15.66.4 r4).
           MOVE FUNCTION LENGTH(FUNCTION DISPLAY-OF(N-SRC)) TO LEN-R.
           DISPLAY "LEN-D=" LEN-R.
           MOVE FUNCTION LENGTH(FUNCTION NATIONAL-OF(A-SRC)) TO LEN-R.
           DISPLAY "LEN-N=" LEN-R.
           STOP RUN.
