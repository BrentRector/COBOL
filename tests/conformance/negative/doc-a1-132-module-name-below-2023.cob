      *> reject-at: 85 2002 2014
      *> The REJECT half of 2023/doc_a1_132_module_name_stack_deep
      *> (docs/CONFORMANCE.md DOC-A.1-132). The MODULE-NAME function
      *> (ISO 15.65) is an ISO/IEC 1989:2023 introduction, so an earlier
      *> edition refuses the reference rather than evaluate it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DOCA1132N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 L     PIC 9(9).
       PROCEDURE DIVISION.
           MOVE FUNCTION LENGTH(FUNCTION MODULE-NAME(STACK)) TO L
           DISPLAY "L=" L
           STOP RUN.
