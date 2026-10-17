*>--- conformance-case
*> id: pb305-numval-c-edited-category
*> rules: [AR-15.37.3-2, AR-15.48.3-3, AR-15.59.3-2, AR-15.63.3-2, AR-15.68.3-2, AR-15.71.3-3, AR-15.72.3-3, AR-15.79.3-3, AR-15.87.3-2, AR-15.92.3-2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB305 - THE CLASS PROJECTION MUST NOT LEAK INTO A CATEGORY-WORDED RULE.
*> 15.68.3 is the one function in the catalogue that asks BOTH questions of the same operand: r1 is
*> CATEGORY-worded - "Argument-1 shall be of category alphanumeric or national" - while r2 is CLASS-worded -
*> "Argument-2, if specified, shall be of the same class as argument-1". 8.5.2.1's closing sentence is what
*> keeps them apart: "Use of the name of a data class or data category in the rules of COBOL refers to the
*> category unless class is specifically indicated."
*> So a PIC ZZ9 item, whose CATEGORY is numeric-edited and whose CLASS is alphanumeric, is admitted by r2's
*> class test and REFUSED by r1's category test - and the refusal is the one that must survive. Before
*> PB305 this program was rejected TWICE, once correctly on the category and once on a class disagreement
*> that 8.5.2.1 Table 2 says does not exist; the class complaint is gone and the category one remains.
IDENTIFICATION DIVISION.
PROGRAM-ID. NEGPB305NVCCAT.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 ED PIC ZZ9.
01 N  PIC 9(4)V99.
PROCEDURE DIVISION.
MAIN.
    MOVE 5 TO ED.
    COMPUTE N = FUNCTION NUMVAL-C(ED "$").
    STOP RUN.
