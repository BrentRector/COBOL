*>--- conformance-case
*> id: pb238-by-value-formal-derived-literal
*> rules: [SR-14.9.4.3-23]
*> source-format: free
*> portable: unverified
*>---
*> ISO 14.9.4.3 SR23's SECOND subject (kb/Work PB238): "...or its corresponding formal parameter is
*> specified with the BY VALUE phrase". Here the argument carries NO phrase at all - 14.9.4.4 GR9 b)
*> derives BY VALUE from the corresponding formal - so SR23 governs a literal-2 the source never marked.
*> This leg was doubly unreachable before PB238: the bare literal arm hard-coded BY CONTENT (the VALUE
*> outcome happened by the copy-out skip, not by GR9 b)'s mechanism), and nothing consulted the formal's
*> declared mode. The constant-name spelling is the same rule: 13.10.4 GR1 makes K substitute the
*> alphanumeric literal "XY" and GR2 gives it that literal's class, so SR23 screens the SUBSTITUTED
*> literal, never the name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB238N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 K CONSTANT AS "XY".
       PROCEDURE DIVISION.
       MAIN.
           CALL "PB238N2S" AS NESTED USING K
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB238N2S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LX PIC S9(4).
       PROCEDURE DIVISION USING BY VALUE LX.
       M1.
           GOBACK.
       END PROGRAM PB238N2S.
       END PROGRAM PB238N2.
