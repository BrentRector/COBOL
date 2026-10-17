      *> ISO §8.3.2.2: A user-defined word is a COBOL word that is supplied by the user to satisfy the format of a
      *>   clause or statement. (cite.py --check 8.3.2.2 -> OK) A word the program supplies is a user-defined word
      *>   whatever the .NET runtime calls one of its own types: nothing in the standard reserves CobolString,
      *>   ProgramRegistry, HostLanding, CobolFile, Int128, Version ... (kb/Work PB3270).
      *> The compiler's generated C# names those runtime types by SIMPLE name (CobolString.Store, HostLanding.LandText,
      *>   ProgramRegistry.CallProgram, Int128), so a data item, a group member, a formal parameter, a file or an
      *>   index spelled like one must get a different C# name, or the generated source is rejected (CS0236, CS0120,
      *>   CS1061) and legal COBOL fails to compile. The callee is itself named HostLanding: its host-entry class
      *>   would capture the simple name for the whole namespace.
      *> Each leg can fail: an unrenamed item does not compile; the CALL passes ProgramRegistry and CobolRec (a
      *>   group with members CobolNum and StorageCell) by reference, so the callee's changes are the caller's.
      *> Derivation (ISO §14.9.11.4 GR1 and GR6, cite.py --check -> OK: the operands are transferred in order, their
      *>   conversion to characters being the implementor's, docs/CONFORMANCE.md): CobolString PIC X(5) VALUE AB
      *>   -> AB and 3 spaces; ProgramRegistry CD and 3 spaces; CobolNum 007; StorageCell Q and 2 spaces. The
      *>   callee shows IN, then the three values with separating spaces; it moves EF to the first formal, adds 1 to
      *>   CobolNum (008) and moves R to StorageCell. Int128 PIC 9(3) COMP VALUE 5 shows 005; Version 41 plus 1 is 42;
      *>   Func (2) is 6; RENAMES Version THRU CobolIndex shows 42A; the record written to CobolFile is read back.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3270DR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT CobolFile ASSIGN TO "pb3270dr.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD CobolFile.
       01 CobolReport PIC X(10).
       WORKING-STORAGE SECTION.
       01 CobolString PIC X(5) VALUE "AB".
       01 ProgramRegistry PIC X(5) VALUE "CD".
       01 CobolRec.
          05 CobolNum PIC 9(3) VALUE 7.
          05 StorageCell PIC X(3) VALUE "Q".
       77 Int128 PIC 9(3) COMP VALUE 5.
       01 Convert.
          05 Version PIC 9(2) VALUE 41.
          05 CobolIndex PIC X VALUE "A".
             88 CobolBool VALUE "A".
          05 Func PIC 9 OCCURS 3 TIMES INDEXED BY CobolTable.
       66 BitConverter RENAMES Version THRU CobolIndex.
       PROCEDURE DIVISION.
       CobolSort SECTION.
       ProgramReturn.
           DISPLAY CobolString ProgramRegistry CobolNum StorageCell
           MOVE "XY" TO CobolString
           DISPLAY CobolString
           CALL "HostLanding" USING ProgramRegistry CobolRec
           DISPLAY ProgramRegistry CobolNum StorageCell
           DISPLAY Int128
           ADD 1 TO Version
           DISPLAY Version
           SET CobolTable TO 2
           MOVE 6 TO Func (CobolTable)
           DISPLAY Func (2)
           IF CobolBool DISPLAY "COND" END-IF
           DISPLAY BitConverter
           OPEN OUTPUT CobolFile
           MOVE "line one" TO CobolReport
           WRITE CobolReport
           CLOSE CobolFile
           OPEN INPUT CobolFile
           READ CobolFile
           CLOSE CobolFile
           DISPLAY CobolReport
           STOP RUN.
       END PROGRAM PB3270DR.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. HostLanding.
       DATA DIVISION.
       LINKAGE SECTION.
       01 ProgramRegistry PIC X(5).
       01 CobolRec.
          05 CobolNum PIC 9(3).
          05 StorageCell PIC X(3).
       PROCEDURE DIVISION USING ProgramRegistry CobolRec.
           DISPLAY "IN " ProgramRegistry " " CobolNum " " StorageCell
           MOVE "EF" TO ProgramRegistry
           ADD 1 TO CobolNum
           MOVE "R" TO StorageCell
           EXIT PROGRAM.
       END PROGRAM HostLanding.
