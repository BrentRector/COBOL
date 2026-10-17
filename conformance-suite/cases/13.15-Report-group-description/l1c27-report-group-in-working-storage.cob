      *>--- conformance-case
      *> id: l1c27-report-group-in-working-storage
      *> rules: [SR-13.15.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.15.3 1) — a report group entry outside the report
      *> section
      *> "Report group description entries may appear only in the report
      *> section."
      *> cite.py --check 13.15.3 "Report group description entries may
      *>   appear only in the report section" -> OK §13.15.3 1)
      *> WX is a complete, well-formed report group description entry
      *> (TYPE clause Format 2, §13.18.57.2 "TYPE IS ... DETAIL"; a LINE
      *> entry; a COLUMN/PIC/SOURCE entry beneath it) written in the
      *> WORKING-STORAGE SECTION. In the report section it would be
      *> legal, so its placement is the only reason to reject it. DETAIL
      *> is a reserved word, so at 2002 and later the entry cannot be
      *> read as TYPE clause Format 1 (TYPE TO type-name-1) either.
      *> This is a placement (pure-syntax) rule: the refusal is the
      *> generic parse diagnostic at the first report-group-only word of
      *> the entry.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C27E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WA PIC X VALUE "A".
       01 WX TYPE IS DETAIL.
          03 LINE PLUS 1.
             05 COLUMN 1 PIC X SOURCE WA.
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN.
