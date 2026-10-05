      *> PB1123 / GR-14.6.4-1 - ISO 14.6.4 lists "1) locale identification" first in the item identification of an
      *>   identifier, and 14.6.6 r6 says what that identification yields for an edited item: "For a data item
      *>   described with a LOCALE phrase, if a locale-name is specified, locale category LC_MONETARY in that
      *>   locale is used for editing and de-editing of the data item. If a locale-name is not specified, category
      *>   LC_MONETARY in the locale current at the time of editing or de-editing is used."
      *>   (cite.py --check 14.6.6 -> OK 6) and 9); 14.6.4 -> OK 1))
      *> So an item with NO locale-name uses the locale in force WHEN IT IS EDITED, and editing a receiving item
      *> happens after the identifier's subscript (item identification) has been evaluated. A function in the
      *> subscript that switches the locale (a callee's SET LOCALE stands for the run unit, 14.6.6 r9) therefore
      *> changes the locale the edit uses: the locale is not captured at the start of the identifier.
      *> L1 - under the en-US locale the edited value is 1,234.50 (',' grouping, '.' decimal separator).
      *> L2 - MOVE N TO A(FUNCTION SWLOC): SWLOC switches LC_MONETARY to de-DE and returns 1; A(1) is edited
      *>      afterwards, in de-DE: 1.234,50.  Both are shown trimmed so the answer does not depend on how the
      *>      implementor aligns the edited result inside SIZE.
      *>
      *>   L1=1,234.50
      *>   L2=1.234,50
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB1123SWLOC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE GER IS "de-DE".
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9.
       PROCEDURE DIVISION RETURNING R.
           SET LOCALE LC_MONETARY TO GER
           MOVE 1 TO R
           GOBACK.
       END FUNCTION PB1123SWLOC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1123LOC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           LOCALE ENG IS "en-US".
       REPOSITORY.
           FUNCTION PB1123SWLOC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 LOC-TAB.
           05 A PIC ZZZZ9.99 LOCALE SIZE IS 12 OCCURS 2.
       01 N PIC 9(5)V99 VALUE 1234.50.
       PROCEDURE DIVISION.
           SET LOCALE LC_MONETARY TO ENG
           MOVE N TO A(1)
           DISPLAY "L1=" FUNCTION TRIM(A(1))
           MOVE N TO A(FUNCTION PB1123SWLOC)
           DISPLAY "L2=" FUNCTION TRIM(A(1))
           STOP RUN.
       END PROGRAM PB1123LOC.
