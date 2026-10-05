*> reject-at: 85 2002 2014 2023
*> kb/Work PB1407 / PB1458. ISO 8.4.3.3.2 prints the reference-modification general format identifier-1( leftmost-position :
*> [ length ] ): only the LENGTH is bracketed, so the leftmost-position is required and a colon cannot lead. X (:2), X (: 2)
*> and X (:) are refused COBOLNET2876 on every surface that reads a reference modifier: a sending operand (DISPLAY), a
*> RECEIVING operand (MOVE ... TO X (:2) used to be dropped from its statement with no diagnostic at all) and a relation
*> operand (the last two used to draw only the COBOLNET2362 internal-error net). X (1:2) and X (3:) are the legal spellings.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1407RMN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(6) VALUE "ABCDEF".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY X (1:2)
           DISPLAY X (3:)
           DISPLAY X (:2)
           MOVE "ZZ" TO X (: 2)
           IF X (:) = "AB"
               DISPLAY "EQ"
           END-IF
           STOP RUN.
