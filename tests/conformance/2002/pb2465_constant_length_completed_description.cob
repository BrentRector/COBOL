      *> kb/Work PB2465 - a constant's LENGTH OF / BYTE-LENGTH OF measures
      *> the operand AS DESCRIBED (ISO 13.10, 2002). 13.10.4 GR5 / GR6: "The
      *> value of constant-name-1 is determined as specified in the
      *> BYTE-LENGTH intrinsic function" / "... the LENGTH intrinsic
      *> function", so every clause that decides the operand's size applies
      *> first, wherever it is written:
      *>   KP  = 3  BYTE-LENGTH OF PV: 13.18.60.4 GR1, the group's USAGE
      *>            PACKED-DECIMAL "applies ... to each elementary item in
      *>            the group" - S9(5) packed, 3 bytes
      *>   KV  = 8  BYTE-LENGTH OF V: the group's USAGE NATIONAL - 9(4)
      *>            national, 2 bytes per national character
      *>   KVL = 4  LENGTH OF V: 4 national character positions
      *>   KG  = 6  LENGTH OF G: 13.18.57.4 GR1, TYPE T "as though the data
      *>            description identified by type-name-1 had been coded in
      *>            place" - X(5) + Y X(1); T is declared AFTER B demands KG
      *>   KI  = 5  LENGTH OF VI: 13.16.3 SR9, VALUE "ABCD" implies PIC X(4)
      *>   KS  = 4  LENGTH OF S: 13.18.52.4 GR1, the group's SIGN LEADING
      *>            SEPARATE applies to N - S9(3) plus a separate sign
      *>   KW  = 4  LENGTH OF W, inside the record R its constant is
      *>            demanded by (A PIC X(KW)): W inherits R's SIGN clause
      *>   KS3 = 3  BYTE-LENGTH OF G3: 13.18.49.4 GR3, the USAGE of SW's
      *>            alphanumeric group applies to S3 SAME AS SW
      *> Each value is displayed beside the intrinsic it is defined by, and
      *> A, B and C are described with the constants. Before the fix the
      *> operands were measured before the description-completion passes
      *> (measured: a national group's leaf 4 bytes, a record holding a
      *> TYPE'd member 1, a group SIGN SEPARATE leaf 3), in silence. The
      *> negative half: constants are 2002, refused at 85
      *> (negative/constant-at-85).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2465CD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE PACKED-DECIMAL.
          05 PV PIC S9(5).
       01 KP CONSTANT AS BYTE-LENGTH OF PV.
       01 H USAGE NATIONAL.
          05 V PIC 9(4).
       01 KV CONSTANT AS BYTE-LENGTH OF V.
       01 KVL CONSTANT AS LENGTH OF V.
       01 G.
          05 X TYPE T.
          05 Y PIC X.
       01 B PIC X(KG).
       01 KG CONSTANT AS LENGTH OF G.
       01 T TYPEDEF PIC X(5).
       01 VI.
          05 VW VALUE "ABCD".
          05 VY PIC X.
       01 KI CONSTANT AS LENGTH OF VI.
       01 S SIGN IS LEADING SEPARATE.
          05 N PIC S9(3).
       01 KS CONSTANT AS LENGTH OF S.
       01 R SIGN IS LEADING SEPARATE.
          05 A PIC X(KW).
          05 W PIC S9(3).
       01 KW CONSTANT AS LENGTH OF W.
       01 SRC USAGE PACKED-DECIMAL.
          05 SW PIC S9(5).
       01 G3.
          05 S3 SAME AS SW.
       01 KS3 CONSTANT AS BYTE-LENGTH OF G3.
       01 C PIC X(KS3).
       PROCEDURE DIVISION.
           DISPLAY "KP=" KP " " FUNCTION BYTE-LENGTH(PV)
           DISPLAY "KV=" KV " " FUNCTION BYTE-LENGTH(V)
               " KVL=" KVL " " FUNCTION LENGTH(V)
           DISPLAY "KG=" KG " " FUNCTION LENGTH(G)
               " B=" FUNCTION LENGTH(B)
           DISPLAY "KI=" KI " " FUNCTION LENGTH(VI)
           DISPLAY "KS=" KS " " FUNCTION LENGTH(S)
           DISPLAY "KW=" KW " " FUNCTION LENGTH(W)
               " A=" FUNCTION LENGTH(A)
           DISPLAY "KS3=" KS3 " " FUNCTION BYTE-LENGTH(G3)
               " C=" FUNCTION LENGTH(C)
           STOP RUN.
