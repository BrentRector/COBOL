*>--- conformance-case
*> id: pb551_constant_record_value_content
*> rules: [SR-13.18.63.3-25]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO/IEC 1989:2023 13.18.63.3 SR25 (kb/Work PB551) forbids a format 3, 4 or 5 VALUE clause in or under a
      *> CONSTANT RECORD entry - and ONLY those formats. The FORMAT-1 VALUEs of the record's own entries are its
      *> content (13.18.15.4), and a condition-name on an item OUTSIDE the constant record is untouched.
      *> WHY EACH LINE CAN FAIL:
      *>   CR=      the record's format-1 content, [7AB]; a screen barring every VALUE under the record refuses it.
      *>   FLAG=    Y - the level-88 on W (not in the constant record) still binds and evaluates.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB551CRPOS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CR CONSTANT RECORD.
           05 X PIC 9 VALUE 7.
           05 Y PIC X(2) VALUE "AB".
       01 W PIC 9 VALUE 7.
           88 W-SEVEN VALUE 7.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "CR=[" X Y "]"
           IF W-SEVEN DISPLAY "FLAG=Y" ELSE DISPLAY "FLAG=N" END-IF
           STOP RUN.
