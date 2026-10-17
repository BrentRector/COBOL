*>--- conformance-case
*> id: pb664-string-sender-pointer
*> rules: [SR-14.9.43.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB664's OWN repro, one statement over from the MOVE the note was filed for.
      *> ISO 14.9.43.3 SR1 - identifier-1 shall be "described implicitly or explicitly as usage display or
      *> national"; a data-pointer item is neither, and ISO 13.18.60.3 SR9 lists the contexts a data-pointer
      *> "may be referenced explicitly" in - a CALL, INITIALIZE, INVOKE or SET statement, a relation condition,
      *> a procedure division header, an inline method invocation's argument list, a function-identifier
      *> argument, an ALLOCATE RETURNING phrase or a FREE statement.  STRING is not among them.
      *> Measured before the fix: this program compiled clean and stored "CobolNet.Runtime.CellPointer" - the
      *> CLR carrier's type name - into A66, through OperandText's sending-image fall-through.
      *> USAGE POINTER is a COBOL-2002 introduction, so 85 rejects the DECLARATION instead (COBOLNET0902) and
      *> is not claimed here.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB664NEGP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P   USAGE POINTER.
       01 TGT PIC X(4).
       01 A66 PIC X(40).
       PROCEDURE DIVISION.
           SET P TO ADDRESS OF TGT
           MOVE SPACES TO A66
           STRING P DELIMITED BY SIZE INTO A66
           DISPLAY "A66=[" A66 "]"
           STOP RUN.
