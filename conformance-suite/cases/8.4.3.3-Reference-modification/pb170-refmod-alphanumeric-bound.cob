*>--- conformance-case
*> id: pb170-refmod-alphanumeric-bound
*> rules: [SR-8.4.3.3.3-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.4.3.3.3 syntax rule 4: "Leftmost-position and length shall be
      *> arithmetic expressions" - the reference-modification twin of the
      *> subscript rule, reached through the IDENTICAL ResolveSubscriptName ->
      *> PositionRead path, and unscreened for the same reason.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB170N5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 XE PIC X(4) VALUE "0002".
       01 W  PIC X(5) VALUE "ABCDE".
       01 R  PIC X(2).
       PROCEDURE DIVISION.
       MAIN.
           MOVE W(XE:2) TO R
           STOP RUN.
