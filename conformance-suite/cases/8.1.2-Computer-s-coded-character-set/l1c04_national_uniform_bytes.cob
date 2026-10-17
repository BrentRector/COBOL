      *>--- conformance-case
      *> id: l1c04_national_uniform_bytes
      *> rules: [GR-8.1.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §8.1.2 GR2 — every national character takes the same
      *>   number of bytes, fixed at compile time.
      *> "The number of bytes used in the memory of the computer to
      *>  represent characters in the national coded character set shall
      *>  be the same for all characters in that coded character set;
      *>  the number of bytes shall be determined at compile time."
      *> cite.py --check 8.1.2 "The number of bytes used in the memory
      *>   of the computer to represent characters in the national coded
      *>   character set shall be the same for all characters in that
      *>   coded character set" -> OK §8.1.2 2)
      *> cite.py --check 13.18.60.4 "Each implementor shall specify the
      *>   size and representation of characters stored for usage
      *>   NATIONAL" -> OK §13.18.60.4 8)  - the documented size is 2
      *>   (docs/CONFORMANCE.md DOC-A.1-213: one UTF-16 code unit).
      *> cite.py --check 15.14.4 "The returned value is an integer that
      *>   is the length of argument-1 in number of bytes"
      *>   -> OK §15.14.4 1)
      *> Derivation (b = the uniform byte count, 2 per DOC-A.1-213):
      *>   "A=02 E=02 J=02"  PIC N items holding U+0041, U+20AC, U+65E5
      *>                     - one character each, so b each, EQUAL.
      *>   "LA=02 LE=02"     national LITERALS N"A" and N"€": b each.
      *>   "N4=08"           PIC N(4): 4 positions x b.
      *>   "N4=08"           after MOVE N"€日€日" the byte count is
      *>                     unchanged - fixed at compile time, not
      *>                     by the content stored.
      *>   "OV=A€日B"        X(8) REDEFINES N(4) must span exactly
      *>                     4 x b bytes; MOVE back into the N(4)
      *>                     overlay restores every character, so each
      *>                     of the mixed characters used b bytes.
      *>   "SAME"            BYTE-LENGTH(N-A) = BYTE-LENGTH(N-J).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C04G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N-A PIC N VALUE N"A".
       01 N-E PIC N VALUE N"€".
       01 N-J PIC N VALUE N"日".
       01 N4G.
          05 N4 PIC N(4) VALUE N"A€日B".
       01 N4X REDEFINES N4G PIC X(8).
       01 SAVE-X PIC X(8).
       01 L1 PIC 99.
       01 L2 PIC 99.
       01 L3 PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION BYTE-LENGTH(N-A) TO L1.
           MOVE FUNCTION BYTE-LENGTH(N-E) TO L2.
           MOVE FUNCTION BYTE-LENGTH(N-J) TO L3.
           DISPLAY "A=" L1 " E=" L2 " J=" L3.
           MOVE FUNCTION BYTE-LENGTH(N"A") TO L1.
           MOVE FUNCTION BYTE-LENGTH(N"€") TO L2.
           DISPLAY "LA=" L1 " LE=" L2.
           MOVE FUNCTION BYTE-LENGTH(N4) TO L1.
           DISPLAY "N4=" L1.
           MOVE N4X TO SAVE-X.
           MOVE N"€日€日" TO N4.
           MOVE FUNCTION BYTE-LENGTH(N4) TO L1.
           DISPLAY "N4=" L1.
           MOVE SAVE-X TO N4X.
           DISPLAY "OV=" N4.
           IF FUNCTION BYTE-LENGTH(N-A) = FUNCTION BYTE-LENGTH(N-J)
               DISPLAY "SAME"
           ELSE
               DISPLAY "DIFFERENT"
           END-IF.
           STOP RUN.
