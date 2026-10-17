      *>--- conformance-case
      *> id: l1c17_linkage_based_access
      *> rules: [GR-13.7.4-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.7.4 GR1 — a BASED linkage item is accessed per 13.18.5
      *> "The access to a based data item is described in 13.18.5,
      *>  BASED clause."
      *> cite.py --check 13.7.4 "The access to a based data item is
      *>   described in 13.18.5, BASED clause." -> OK §13.7.4 1)
      *> cite.py --check 13.7.3 "A based data item may be referenced as
      *>   described in 13.18.5, BASED clause" -> OK §13.7.3 4)
      *> cite.py --check 13.18.5.4 "The implicit data-address pointer
      *>   has an initial value of NULL." -> OK §13.18.5.4 2)
      *> cite.py --check 8.6.5 "An association is established linking
      *>   the based entry to actual data when its implicit data-address
      *>   pointer is assigned the address of an existing data item or
      *>   assigned the address of storage obtained with an ALLOCATE
      *>   statement." -> OK §8.6.5
      *> cite.py --check 14.9.3.4 "if data-name-1 is specified, the
      *>   address of the based data item referenced by data-name-1 is
      *>   set to the address of that storage." -> OK §14.9.3.4 4)
      *> The rule delegates: a LINKAGE item with BASED is NOT reached
      *> through a formal-parameter correspondence (this main program
      *> has no USING at all - legal, §13.7.3 SR4's based exemption)
      *> but through its implicit data-address pointer.
      *>
      *> DERIVED OUTPUT:
      *>   Y-INIT=NULL  §13.18.5.4 GR2: the pointer starts as NULL.
      *>   Y=ABC        after SET ADDRESS OF L-Y TO ADDRESS OF W-ONE the
      *>                based item IS W-ONE's storage (§8.6.5).
      *>   W=XYZ        so a MOVE into L-Y changes W-ONE.
      *>   Y=DEF        SET to ADDRESS OF W-TWO re-associates L-Y.
      *>   Y-OFF=NULL   SET ADDRESS OF L-Y TO NULL ends the association.
      *>   X=QR42       ALLOCATE L-X (§14.9.3.4 GR4 b)) gives it fresh
      *>                storage; its subordinates are then usable.
      *>   X-ADDR=SET   and its address is no longer NULL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C17L.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-ONE PIC X(3) VALUE "ABC".
       01 W-TWO PIC X(3) VALUE "DEF".
       LINKAGE SECTION.
       01 L-Y PIC X(3) BASED.
       01 L-X BASED.
          05 L-X1 PIC XX.
          05 L-X2 PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           IF ADDRESS OF L-Y = NULL
               DISPLAY "Y-INIT=NULL"
           ELSE
               DISPLAY "Y-INIT=SET"
           END-IF.
           SET ADDRESS OF L-Y TO ADDRESS OF W-ONE.
           DISPLAY "Y=" L-Y.
           MOVE "XYZ" TO L-Y.
           DISPLAY "W=" W-ONE.
           SET ADDRESS OF L-Y TO ADDRESS OF W-TWO.
           DISPLAY "Y=" L-Y.
           SET ADDRESS OF L-Y TO NULL.
           IF ADDRESS OF L-Y = NULL
               DISPLAY "Y-OFF=NULL"
           ELSE
               DISPLAY "Y-OFF=SET"
           END-IF.
           ALLOCATE L-X.
           MOVE "QR" TO L-X1.
           MOVE 42 TO L-X2.
           DISPLAY "X=" L-X.
           IF ADDRESS OF L-X NOT = NULL
               DISPLAY "X-ADDR=SET"
           ELSE
               DISPLAY "X-ADDR=NULL"
           END-IF.
           STOP RUN.
