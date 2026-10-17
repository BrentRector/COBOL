*>--- conformance-case
*> id: pb485-level-number-missing
*> rules: [SR-13.18.33.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.33.3 SR1: "A level-number is required as the first
      *> element in each data description or screen description entry."
      *> The remaining arm of the clause, and the one arm that is enforced
      *> STRUCTURALLY rather than by the COBOLNET1746 screen: every
      *> grammar rule that spells an entry -- dataDescriptionEntry,
      *> linkageProcedureParameter, reportGroupEntry,
      *> screenDescriptionEntry -- opens with a mandatory levelNumber, so
      *> an entry without one cannot parse and the requirement cannot be
      *> evaded. This witness exists because a rule enforced by the
      *> grammar's SHAPE has no code site to point at, and an unwitnessed
      *> structural claim is indistinguishable from an unchecked one.
      *> kb/Work PB485.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB485N7.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  V PIC X(4) VALUE "ABCD".
       W PIC X(3).
       PROCEDURE DIVISION.
           DISPLAY V
           STOP RUN.
