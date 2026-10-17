*>--- conformance-case
*> id: pb944-bit-group-to-numeric
*> rules: [GR-14.9.25.4-4]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB944 - THE BIT GROUP HALF OF 14.9.25.4 GR4's SECOND SENTENCE, MEASURED AS A REFUSAL.
      *> ISO 14.9.25.4 GR4 - "Bit group items and national group items are treated as elementary items in the
      *>   MOVE statement", and 13.18.29.4 GR1 b) makes a GROUP-USAGE BIT group an elementary boolean item of
      *>   PICTURE 1(m). So MOVE BG TO R-NUM is an ELEMENTARY move of a class-boolean sender, and 14.9.25.3
      *>   SR10's Table 16 gives the Boolean row "No" in the "Numeric, Numeric-edited" column: the statement is
      *>   refused at compile time, never run as a store of the group's storage byte.
      *> ⚠ PB944's own probe declared its BG WITHOUT a GROUP-USAGE clause, which by 13.18.29.4 GR3 makes it an
      *>   ALPHANUMERIC group, and GR4's group move of its storage byte into PIC 9(3) is conforming. This case
      *>   is the shape the note's rule actually governs. It is legal source at no edition: GROUP-USAGE is a
      *>   2002 introduction, and below it the entry is refused by the edition gate instead.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB944NEGBITNUM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N               PIC 9 VALUE 1.
       01 BG GROUP-USAGE BIT.
          05 BE           PIC 1 OCCURS 0 TO 8 DEPENDING ON N.
       01 R-NUM           PIC 9(3) VALUE 777.
       PROCEDURE DIVISION.
           MOVE BG TO R-NUM
           STOP RUN.
