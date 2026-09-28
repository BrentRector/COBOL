*> reject-at: 2002 2014 2023
*> kb/Work PB423, the RECEIVING arm of ISO 1989:2023 14.9.25.3 SR1: "The class of identifier-1 or
*> identifier-2 shall not be index, message-tag, object, or pointer." The rule names BOTH operand positions,
*> and this case is the one the compiler's two-arm dispatch left unfixed - the sending arm had already been
*> routed through the 8.5.2.1 Table-2 class reader while the receiving arm still asked one hand-written USAGE
*> (Usage.Index).
*>
*> Measured before the fix: `MOVE NULL TO P` reached the BACKEND and surfaced a Roslyn message about the
*> GENERATED C# - "error CS0029: Cannot implicitly convert type 'string' to
*> 'CobolNet.Runtime.ManagedPointer'" - in place of a COBOL diagnostic. 13.18.60.3 SR9 is the rule the
*> programmer wants instead: a data-pointer data item may be referenced "in a SET statement", never a MOVE.
*>
*> The sender is an alphanumeric data item, a legal MOVE sender, so the receiver is the ONLY violation: `MOVE NULL
*> TO P` (this case's original spelling) now stops at the sender, which ISO 8.4.3.10.3 SR1 refuses first
*> (COBOLNET2576, kb/Work PB1427), and would no longer witness the receiving arm.
*>
*> The edition band starts at 2002 because USAGE POINTER is a COBOL-2002 element (13.18.60); below it the
*> declaration itself is gated and the program would be rejected for a different reason.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB423PTRRECV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       01 A PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
           MOVE A TO P
           STOP RUN.
