*>--- conformance-case
*> id: pb390-goto-unknown-procedure
*> rules: []
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB390 - GO TO states no syntax rule of its own about procedure-name-1 (14.9.17.3 is about
*> identifier-1 and statement position), so the general rules decide: 8.4.2.1 - "In order to use a
*> resource, a statement shall contain a reference that uniquely identifies that resource" - with 8.4.6.1,
*> which lists paragraph-name and section-name among the words that "may be referenced only by statements
*> in the source element in which the user-defined word is declared". The GO TO carrier was the sibling of
*> PERFORM's: same BoundUnsupported, same run-time abort under a message naming a COBOL.NET gap.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB390GOTOUNK.
PROCEDURE DIVISION.
MAIN.
    GO TO NO-SUCH-PARA.
P1.
    DISPLAY "P1".
    STOP RUN.
