      *> kb/Work PB1134 - ISO 14.6.2.3.2 action 1 (cite.py OK: "the storage allocated for the implied or associated sections is set to the specified-fill-character")
      *> with the owner decision kb/Work R53: EVERY storage position of the named sections takes the fill, numeric items included.  OPTIONS INITIALIZE ALL TO X"5A".
      *>   IXA  USAGE INDEX (8 storage bytes, PicInfo.StorageWidth) -> every byte X"5A" -> FUNCTION ORD (ordinal = byte + 1) of byte 1 and byte 8 = 91 91
      *>   IXB  PIC X                                                -> "Z" (X"5A")
      *>   NN   PIC 9(2) USAGE NATIONAL                              -> two national positions, each the fill character "Z" -> ZZ
      *> BEFORE: the INDEX item kept its zero (ORD 1 1) because an INDEX cell had no seed.  The national numeric item is the PB1466 promotion.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1134IX.
       OPTIONS.
           INITIALIZE ALL TO X"5A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NN PIC 9(2) USAGE NATIONAL.
       01 IG.
          05 IXA USAGE INDEX.
          05 IXB PIC X.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "NN=[" NN "]".
           DISPLAY "IXB=[" IXB "]".
           DISPLAY "IX=" FUNCTION ORD(IG(1:1)) " " FUNCTION ORD(IG(8:1)).
           STOP RUN.
