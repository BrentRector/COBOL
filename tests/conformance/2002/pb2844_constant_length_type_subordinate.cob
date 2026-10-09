      *> kb/Work PB2844 - a constant's LENGTH OF / BYTE-LENGTH OF operand may name a subordinate that a
      *> TYPE clause supplies (ISO 13.18.57.4 GR2 a): "the subject of the entry is a group whose subordinate
      *> elements have the same names, descriptions, and hierarchy as the subordinate elements of
      *> type-name-1"), whether the subject is described before the constant, after it, or later inside
      *> the very record whose description demands the constant. Expected (13.10.4 GR6 the LENGTH value,
      *> GR5 the BYTE-LENGTH value):
      *>   K1 = 5   LENGTH OF M OF V; T's M is PIC X(5)
      *>   K2 = 2   BYTE-LENGTH OF N OF V; T's N is PIC 9(4) COMP-5, two bytes
      *>   K3 = 5   LENGTH OF M OF I OF W OF R, through the nested TYPE T2 (I TYPE T)
      *>   K4 = 9   BYTE-LENGTH OF W OF R: T2 = H 2 + I (M 5 + N 2)
      *>   KL = 5   LENGTH OF M OF VL, VL described after the constant
      *>   KO = 5   LENGTH OF M OF VO, VO later inside the open record RO whose A demands KO
      *>   X is X(5) (demands K1), XL is X(5) (demands KL before VL), RO = 5 + 7 = 12 bytes
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2844TYP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF.
          05 M PIC X(5).
          05 N PIC 9(4) COMP-5.
       01 T2 TYPEDEF.
          05 H PIC X(2).
          05 I TYPE T.
       01 KL CONSTANT AS LENGTH OF M OF VL.
       01 XL PIC X(KL).
       01 V TYPE T.
       01 R.
          05 Q PIC X.
          05 W TYPE T2.
       01 K1 CONSTANT AS LENGTH OF M OF V.
       01 K2 CONSTANT AS BYTE-LENGTH OF N OF V.
       01 K3 CONSTANT AS LENGTH OF M OF I OF W OF R.
       01 K4 CONSTANT AS BYTE-LENGTH OF W OF R.
       01 X PIC X(K1).
       01 RO.
          05 A PIC X(KO).
          05 VO TYPE T.
       01 KO CONSTANT AS LENGTH OF M OF VO.
       01 VL TYPE T.
       PROCEDURE DIVISION.
           DISPLAY K1 " " K2 " " K3 " " K4
           DISPLAY KL " " KO
           DISPLAY FUNCTION LENGTH(X) " " FUNCTION LENGTH(XL)
           DISPLAY FUNCTION BYTE-LENGTH(RO)
           STOP RUN.
