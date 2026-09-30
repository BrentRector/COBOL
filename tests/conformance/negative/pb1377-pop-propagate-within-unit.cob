*> reject-at: 2023
*> ISO/IEC 1989:2023 7.3.20.3 SR2 (cite.py --check 7.3.20.3 "the POP directive shall not be specified where directive-name must not
*> be specified" -> OK 2)) with 7.3.21.3 SR1: PROPAGATE must not be specified within a compilation unit. kb/Work PB1377. Fixed form.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1377N02.
       PROCEDURE DIVISION.
           DISPLAY "A".
       >>POP PROPAGATE
           STOP RUN.
