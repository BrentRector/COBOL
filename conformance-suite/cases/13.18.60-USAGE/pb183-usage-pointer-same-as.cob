*>--- conformance-case
*> id: pb183-usage-pointer-same-as
*> rules: [SR-13.18.60.3-14]
*> source-format: free
*> portable: unverified
*>---
*> ISO 13.18.60.3 SR14 through the FOURTH usage-acquisition route - a SAME AS copy. 13.18.49 GR1
*> copies P's entry description onto Q, manufacturing a level-05 pointer inside an ordinary group.
*> Without this fixture the SAME AS arm can be left unscreened and every other PB183 fixture still
*> passes: the four routes (a written clause, 13.18.60.4 GR1 inheritance, a TYPE clone, a SAME AS
*> copy) are what the ONE ConformanceForest enumeration exists to cover, instead of a hand-written
*> copy of the rule at each site.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB183F.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 P USAGE POINTER.
01 G.
   05 Q SAME AS P.
   05 F PIC X(4).
PROCEDURE DIVISION.
MAIN.
    SET Q TO NULL.
    STOP RUN.
