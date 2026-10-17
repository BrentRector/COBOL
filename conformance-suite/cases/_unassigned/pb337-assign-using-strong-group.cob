*>--- conformance-case
*> id: pb337-assign-using-strong-group
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> ISO 1989:2023 12.4.5.2 syntax rule 7: "Data-name-1 shall reference an alphanumeric data item and
*> shall not be subordinate to the file description entry for file-name-1."
*> An ALPHANUMERIC GROUP ITEM satisfies that -- but 13.18.29.4 general rule 3 says which groups are
*> alphanumeric group items: "If a GROUP-USAGE clause is not specified or implied for a group item
*> that is not strongly typed and is not a variable-length group, that group item is an alphanumeric
*> group item."  A STRONGLY-TYPED group is excluded by name, so ASSIGN ... USING may not reference
*> one.  COBOLNET1810.
*> This fixture is the SIBLING SWEEP of kb/Work PB337, not its subject.  ItemCategory is the one
*> reader of a data item's 8.5.2 category for a rule worded in categories, and its group arm carried
*> only GR3's structural half -- it answered "alphanumeric group item" for ANY group.  Every rule
*> reading it therefore under-rejected a strongly-typed or variable-length group: this one, and the
*> record keys of 12.4.5.12.3 SR2 / 12.4.5.6.3 SR2.  The fix was to write GR3 down once, completely,
*> where the INTO-receiver rule could inherit it too; this program is the witness that the sweep
*> reached a rule that has nothing to do with READ or RETURN.
*> 85 is not listed: TYPEDEF and the STRONG phrase are 2002 introductions.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB337N5.
ENVIRONMENT DIVISION.
INPUT-OUTPUT SECTION.
FILE-CONTROL.
    SELECT SQF ASSIGN USING WS-NAME
        ORGANIZATION IS SEQUENTIAL.
DATA DIVISION.
FILE SECTION.
FD SQF.
01 F-REC PIC X(4).
WORKING-STORAGE SECTION.
01 NT IS TYPEDEF STRONG.
   05 NT-A PIC X(8).
01 WS-NAME TYPE NT.
PROCEDURE DIVISION.
MAIN.
    STOP RUN.
