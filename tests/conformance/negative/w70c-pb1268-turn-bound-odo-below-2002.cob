      *> reject-at: 85
      *> kb/Work PB1268 — THE EDITION FLOOR OF §13.18.38.4 GR7's EC-BOUND-ODO AT AN ELEMENT REFERENCE.
      *>   cite.py --check 13.18.38.4 "If the value of the data item does not fall within the specified bounds, the
      *>     EC-BOUND-ODO exception condition is set to exist" -> OK §13.18.38.4 7)
      *> The rule's observable consequence is the exception condition, and a program can request that it be
      *> checked only from COBOL-2002 onward: the >>TURN directive (§7.3.25) is a 2002 introduction, so at --std 85
      *> the version-conformance pass rejects it (COBOLNET0900). The OCCURS DEPENDING table and its subscripted
      *> reference are legal COBOL-85; delete the first line and it compiles at 85. The positive half is
      *> 2002/w70c_pb1268_ec_bound_odo_element_reference.
       >>TURN EC-BOUND-ODO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W70CN85O.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 99 VALUE 3.
       01 G.
          05 T PIC X OCCURS 2 TO 5 DEPENDING ON N.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "ABC" TO G
           DISPLAY T(1)
           STOP RUN.
