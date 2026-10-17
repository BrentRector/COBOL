*>--- conformance-case
*> id: declined-validate-value-format5-anon
*> rules: [GR-13.16.4-4, GR-13.18.63.4-24]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.16.2 FORMAT 4 (validation): "88 [ condition-name-2 ] value-clause ." - the condition-name is
*> OPTIONAL in this format. Its own witness because the binder's condition-name path returns early on an
*> UNNAMED level-88 (DataBinder.BindCondition needs entry.dataName()), so a binder-hook implementation of
*> COBOLNET1708 would have dropped exactly this shape in silence; the parse-tree pass sees it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLVAL5A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9(4).
          88 VALUE 9 IS INVALID WHEN WS-A = 1.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
