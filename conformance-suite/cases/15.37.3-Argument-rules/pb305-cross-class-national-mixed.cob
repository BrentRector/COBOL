*>--- conformance-case
*> id: pb305-cross-class-national-mixed
*> rules: [AR-15.37.3-2, AR-15.48.3-3, AR-15.59.3-2, AR-15.63.3-2,
*>    AR-15.68.3-2, AR-15.71.3-3, AR-15.72.3-3, AR-15.79.3-3,
*>    AR-15.87.3-2, AR-15.92.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB305 - THE OTHER HALF OF THE PROJECTION, so the fix cannot become a blanket acceptance.
*> 8.5.2.1 Table 2 puts category numeric-edited (usage display) in class ALPHANUMERIC, which is what makes
*> FUNCTION FIND-STRING(<PIC ZZ9 item> "5") legal. It does NOT make it class national, and 15.37.3 r2 is
*> two-armed: "If argument-1 is of class alphabetic or alphanumeric, argument-2 shall be a data item or
*> literal of either class alphabetic or alphanumeric. If argument-1 is of class national, argument-2 shall
*> be of class national." A numeric-edited argument-1 lands in the FIRST arm, so a national argument-2 is
*> refused - exactly as a PIC X(3) argument-1 would refuse it.
*> WHY IT IS HERE: the defect PB305 fixed was a screen that SPLIT class alphanumeric in two. The repair is a
*> Table-2 class PROJECTION, and the failure mode of a projection is over-merging - collapsing national into
*> alphanumeric as well and admitting everything. This case is the witness that it did not.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB305NATMIX.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 ED PIC ZZ9.
01 NA PIC N(4).
01 P  PIC 9(4).
PROCEDURE DIVISION.
MAIN.
    MOVE 5 TO ED.
    MOVE FUNCTION FIND-STRING(ED NA) TO P.
    STOP RUN.
