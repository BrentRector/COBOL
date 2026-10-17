*>--- conformance-case
*> id: pb201_odo_count_carriers
*> rules: [SR-13.18.38.3-17]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB201, THE SECOND EMITTER. The same runtime method, the same bet
      *> on C# overload resolution, and the same CS1503 - but at a site that has
      *> no D18 route to fall back on, because it renders at CODEGEN time from a
      *> Place: RuntimeApi.TableOcc, the OCCURS DEPENDING ON current-count read
      *> that PlaceRenderer.SendingGroupImage slices a group's image to.
      *> ISO 13.18.38.3 rule 17 asks only that "Data-name-1 shall describe an
      *> integer", so a 20-digit COMP item and an unsigned BINARY-DOUBLE are both
      *> legal control items; 13.18.38.4 GR8a then sends "only that part of the
      *> table area that is specified by the value of the data item referenced by
      *> data-name-1". MEASURED on c347a0de - the DEFAULT strict lane, four
      *> errors on this exact program:
      *>   CS1503 Argument 1: cannot convert from 'System.Int128' to 'long'
      *>   CS1503 Argument 1: cannot convert from 'ulong' to 'long'
      *> Expected values: each control item is 2, so each group sends exactly its
      *> first two character positions, space-padded into the PIC X(5) receiver.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB201ODOCARRIERS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N-WIDE PIC 9(20) USAGE COMP VALUE 2.
       01 N-ULNG USAGE BINARY-DOUBLE UNSIGNED.
       01 G1.
          05 T1 PIC X OCCURS 1 TO 5 TIMES DEPENDING ON N-WIDE.
       01 G2.
          05 T2 PIC X OCCURS 1 TO 5 TIMES DEPENDING ON N-ULNG.
       01 RA PIC X(5).
       01 RB PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 2 TO N-ULNG
           MOVE "AB" TO G1
           MOVE "CD" TO G2
           MOVE G1 TO RA
           MOVE G2 TO RB
           DISPLAY "A=[" RA "]"
           DISPLAY "B=[" RB "]"
           STOP RUN.
