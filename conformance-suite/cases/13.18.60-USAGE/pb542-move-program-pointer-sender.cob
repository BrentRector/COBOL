*>--- conformance-case
*> id: pb542-move-program-pointer-sender
*> rules: [SR-13.18.60.3-8]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB542 - THE PROGRAM-POINTER WITNESS for ISO 13.18.60.3 SR8's closed context list. SR8 enumerates
      *> the contexts in which a program-pointer data item may be referenced explicitly, and MOVE is not among
      *> them; ISO 14.9.25.3 SR1 says the same thing from the MOVE statement's side - "The class of identifier-1
      *> or identifier-2 shall not be index, message-tag, object, or pointer" - and ISO 8.5.2.1 Table 2 puts the
      *> program-pointer category in class pointer.
      *> It exists because the row it closes had NO test of its own while the prohibition leaked: `MOVE PP TO N`
      *> escaped the binder and died in the BACKEND as a raw Roslyn `CS1503: cannot convert from
      *> 'CobolNet.Runtime.ProgramPointer' to 'System.Int128'`, an error naming no COBOL rule. The data-pointer
      *> twin (SR9) is witnessed by pb423-move-pointer-sender / pb423-move-pointer-receiver; this is the arm
      *> those two do not cover.
      *> reject-at names 2002 and later because USAGE PROGRAM-POINTER is post-1985 - at --std 85 the data
      *> description entry itself is refused by the edition gate, a different diagnostic.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB542NEGPPMV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PP              USAGE PROGRAM-POINTER.
       01 N               PIC 9(4).
       PROCEDURE DIVISION.
           MOVE PP TO N
           DISPLAY "N=" N
           STOP RUN.
