*> reject-at: 2002 2014 2023
*> ISO/IEC 1989:2023 7.3.13.3 SR5 (cite.py --check 7.3.13.3 ">>WHEN OTHER shall begin on a new line and shall be specified entirely on
*> that line." -> OK 5)) and SR6 (text-2 begins on a new line): a word after OTHER is neither, and the rule holds although a
*> WHEN already matched. kb/Work PB1364. Fixed form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1364N05.
       PROCEDURE DIVISION.
       >>EVALUATE 1
       >>WHEN 1
           DISPLAY "W1".
       >>WHEN OTHER DISPLAY "O"
           DISPLAY "O".
       >>END-EVALUATE
           STOP RUN.
