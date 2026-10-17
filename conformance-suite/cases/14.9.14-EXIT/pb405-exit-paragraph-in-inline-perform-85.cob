*>--- conformance-case
*> id: pb405-exit-paragraph-in-inline-perform-85
*> rules: [GR-14.9.14.4-6]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB405 — the EDITION EDGE of the structured-exit statement this fix is about. EXIT PARAGRAPH is the
*> COBOL-2002 introduction of ISO 14.9.14.2 Format 4 (14.9.14.4 GR6); COBOL-85's EXIT statement has only the
*> bare Format-1 "assign a procedure-name to a given point in a procedure division" form, so at --std 85 the
*> construct row refuses it COBOLNET0900. It compiles at 2002 and above, where the positive witness
*> conformance-suite/cases/14.9.14-EXIT/pb405_exit_paragraph_section_in_inline_perform.cob measures GR6 and GR7 inside an
*> inline PERFORM — the placement that used to emit a bare C# `break` and leave the PERFORM's loop instead of
*> the paragraph.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB405EXIT85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 3
               DISPLAY "IT " I
               IF I = 2
                   EXIT PARAGRAPH
               END-IF
           END-PERFORM
           DISPLAY "TAIL".
       NEXT-P.
           DISPLAY "NEXT".
           STOP RUN.
