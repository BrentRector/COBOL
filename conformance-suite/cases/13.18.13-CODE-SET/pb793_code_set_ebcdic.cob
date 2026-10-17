*>--- conformance-case
*> id: pb793_code_set_ebcdic
*> rules: [GR-13.18.13.4-1, GR-13.18.13.4-2, GR-13.18.13.4-3, GR-13.18.13.4-6, DOC-A.1-183]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 13.18.13.4 GR1 - "The CODE-SET clause identifies alphabets to be used for converting data from
      *> a coded character set on the storage medium to the native character set during input operations,
      *> and from the native character set to the coded character set on the storage medium during output
      *> operations" - and GR6: "a) On input, each coded character from the storage medium is replaced with
      *> its associated native coded character as defined in the alphabet being used. b) On output, each
      *> native coded character in the record is replaced for the storage medium with its associated coded
      *> character as defined in the alphabet being used."
      *>
      *> The alphabet is the EBCDIC code-name (12.3.7.3 SR15 / 12.3.7.4 GR7 i; owner decision kb/Work
      *> PB793), whose correspondence with the native character set is IBM CCSID 37. Before PB793 the only
      *> coded character sets a CODE-SET clause could name here were the ones whose correspondence IS the
      *> identity, and everything else was the documented Annex A A.3 item 27 non-support (COBOLNET1672).
      *>
      *> EXPECTED VALUES, DERIVED FROM CCSID 37 AND NOTHING ELSE. "AB012" written through CODE-SET IS A-EBC
      *> reaches the medium as X'C1 C2 F0 F1 F2' (A=X'C1', B=X'C2', 0=X'F0', 1=X'F1', 2=X'F2'):
      *>   ROUNDTRIP - reading the same file back through the same clause restores the native characters
      *>     (GR6 a undoes GR6 b), so R2 is "AB012".
      *>   MEDIUM - the same physical file read through FR, a file description with NO CODE-SET clause, is
      *>     13.18.13.4 GR7's default ("If the CODE-SET clause is not specified, the native character set is
      *>     assumed for data on the external media"): the bytes arrive unconverted, and FUNCTION ORD gives
      *>     each one's 1-based native ordinal - 194, 195, 241, 242, 243. THIS LEG IS WHAT MAKES THE
      *>     ROUNDTRIP EVIDENCE: without it, a compiler that converted on NEITHER side would also print
      *>     "AB012" and pass.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB793CODESET.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET A-EBC IS EBCDIC.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FE ASSIGN TO "pb793cs.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FR ASSIGN TO "pb793cs.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  FE
           CODE-SET IS A-EBC.
       01  RE           PIC X(5).
       FD  FR.
       01  RR           PIC X(5).
       WORKING-STORAGE SECTION.
       01  R2           PIC X(5).
       01  I            PIC 9.
       01  N1           PIC 999.
       01  LINE-OUT     PIC X(20).
       PROCEDURE DIVISION.
           OPEN OUTPUT FE
           MOVE "AB012" TO RE
           WRITE RE
           CLOSE FE
           OPEN INPUT FE
           READ FE INTO R2 END-READ
           CLOSE FE
           DISPLAY "ROUNDTRIP=[" R2 "]"
           OPEN INPUT FR
           READ FR END-READ
           CLOSE FR
           MOVE SPACES TO LINE-OUT
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 5
               COMPUTE N1 = FUNCTION ORD(RR(I:1))
               DISPLAY "MEDIUM-" I "=" N1
           END-PERFORM
           STOP RUN.
