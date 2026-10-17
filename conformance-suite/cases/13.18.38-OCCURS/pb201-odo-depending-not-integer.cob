*>--- conformance-case
*> id: pb201-odo-depending-not-integer
*> rules: [SR-13.18.38.3-17]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB201 - THE GUARD THAT KEEPS THE SECOND EMITTER'S CARRIER CLAIM
      *> TRUE. RuntimeApi.TableOcc renders the OCCURS DEPENDING current-count read
      *> at CODEGEN time, from a Place, so unlike the subscript renderer it cannot
      *> decline a carrier and re-route to D18 - the capability has to be present
      *> for every carrier that can reach it. That set is bounded by exactly one
      *> rule: ISO 13.18.38.3 rule 17, "Data-name-1 shall describe an integer".
      *> With rule 17 enforced, data-name-1 is a non-float, scale-0 numeric item,
      *> so its carrier is long / Int128 / ulong / UInt128 (or the string image a
      *> whole-group promotion may select) - and CobolTable.Occ declares a
      *> parameter for every one of them. Were rule 17 to stop firing, a COMP-2 or
      *> a PIC 9V9 control item would reach Occ with a double / scaled carrier and
      *> the backend would fail with the same CS1503 this item is about.
      *> N-FRAC is numeric and non-float but SCALE 1, so it does not describe an
      *> integer and is rejected at every edition - OCCURS DEPENDING is COBOL-85
      *> and rule 17 has no introduction axis and no dialect gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB201N4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N-FRAC PIC 9V9 VALUE 2.0.
       01 G.
          05 T PIC X OCCURS 1 TO 5 TIMES DEPENDING ON N-FRAC.
       01 R PIC X(5).
       PROCEDURE DIVISION.
       MAIN.
           MOVE "AB" TO G
           MOVE G TO R
           STOP RUN.
