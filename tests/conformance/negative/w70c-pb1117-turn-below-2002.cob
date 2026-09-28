      *> reject-at: 85
      *> kb/Work PB1117 — THE EDITION FLOOR OF §14.6.13.2 rule 2 ON THE ITEM-IDENTIFICATION LANE.
      *>   cite.py --check 14.6.13.2 "The EC-DATA-INCOMPATIBLE exception condition is set to exist for a class
      *>     condition and a VALIDATE statement when invalid data is detected during item identification"
      *>     -> OK §14.6.13.2 1)
      *> The rule's only observable consequence is the exception condition, and a program can request that it
      *> be checked only from COBOL-2002 onward: the >>TURN directive (§7.3.25) is a 2002 introduction, so at
      *> --std 85 the version-conformance pass rejects it (COBOLNET0900). The subscripted reference below is
      *> legal COBOL-85; delete the first line and it compiles at 85, which is what makes COBOLNET0900 here
      *> attributable to the directive and to nothing else. The positive half is
      *> 2002/w70c_pb1117_item_identification_incompatible.
       >>TURN EC-DATA-INCOMPATIBLE CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W70CN85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 N PIC 9.
       01 TX VALUE "ABCDE".
          05 TE PIC X OCCURS 5.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "2" TO G
           DISPLAY TE(N)
           STOP RUN.
