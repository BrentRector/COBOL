*>--- conformance-case
*> id: pb878-initialize-replacing-vlgroup
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 14.9.20.3 SR4: "For each of the other categories specified in the REPLACING phrase, a MOVE
*> statement with identifier-2 or literal-1 as the sending item and an item of the specified category as the
*> receiving operand shall be valid." That is the WHOLE MOVE-validity question, and 14.9.25.3 SR9 is inside it:
*> "If identifier-1 or identifier-2 references a variable-length group then these groups shall be compatible
*> groups as specified in 8.5.1.12, Variable-length groups." The receiving operand here is "an item of category
*> ALPHANUMERIC" - an elementary item, never a compatible group - so VG as the REPLACING operand is invalid.
*> kb/Work PB878: INITIALIZE asked SR8 and SR10 and never SR9, so this compiled clean and aborted the run unit
*> with a NotImplementedCobolFeatureException. Refused at compile time with COBOLNET2031 (the SR4 framing).
*> Variable-length groups are declarable from COBOL-2014, so the rule is unreachable below it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB878NVL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 VG.
          05 VD PIC X DYNAMIC LENGTH LIMIT IS 10.
       01 G.
          05 A PIC X(5).
       PROCEDURE DIVISION.
           MOVE "AB" TO VD
           INITIALIZE G REPLACING ALPHANUMERIC DATA BY VG
           DISPLAY "A=[" A "]"
           STOP RUN.
