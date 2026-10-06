      *> reject-at: 2002 2014 2023
      *> kb/Work PB2087 - the residue of the area-formal landing. A strongly-typed group whose leaf is a
      *> POINTER has no character image: its pointer value rides the MANAGED SLOT of the storage cell. ISO
      *> 14.2.3 GR8 (cite.py --check 14.2.3 "operates as if the formal parameter occupies the same storage
      *> area as the argument" -> OK) lets a BY REFERENCE argument cross whole, because the formal is laid
      *> over the argument's own cell (conformance:2002/pb2087_by_reference_area_formals). BY CONTENT, GR9's
      *> record is a COPY, and this activation boundary still copies a group as its character image, which
      *> would silently drop the pointer - so the CALL is refused at compile time, naming the reason
      *> (COBOLNET1688), never run with a NULL where the activating element stored an address.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2087NEG1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PT TYPEDEF STRONG GLOBAL.
          05 PP USAGE POINTER.
          05 PX PIC X(2).
       01 PREC TYPE PT.
       PROCEDURE DIVISION.
           CALL "PB2087NEG2" AS NESTED USING BY CONTENT PREC
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2087NEG2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP TYPE PT.
       PROCEDURE DIVISION USING LP.
           GOBACK.
       END PROGRAM PB2087NEG2.
       END PROGRAM PB2087NEG1.
