*>--- conformance-case
*> id: pb423-move-pointer-sender
*> rules: [SR-13.18.60.3-9, SR-14.9.25.3-1]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB423, the SENDING arm of ISO 1989:2023 14.9.25.3 SR1: "The class of identifier-1 or identifier-2
*> shall not be index, message-tag, object, or pointer." 8.5.2.1 Table 2 puts the data-pointer,
*> function-pointer and program-pointer categories in class POINTER, so a USAGE POINTER item is barred from
*> EITHER operand position, whatever the other operand is; 13.18.60.3 SR9 lists the references a data-pointer
*> data item may appear in and a MOVE is not among them.
*>
*> Measured before the fix: this program compiled, ran and printed Y=[CobolNet] - the CLR type name of the
*> pointer carrier, deposited into a user's alphanumeric item with no diagnostic anywhere. The screen asked
*> only class INDEX, excused by a comment asserting the other three classes "cannot reach a bound MOVE yet".
*>
*> The edition band starts at 2002 because USAGE POINTER is a COBOL-2002 element (13.18.60); below it the
*> declaration itself is gated and the program would be rejected for a different reason.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB423PTRSEND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(5) VALUE "HELLO".
       01 P USAGE POINTER.
       01 Y PIC X(8).
       PROCEDURE DIVISION.
           SET P TO ADDRESS OF X
           MOVE P TO Y
           STOP RUN.
