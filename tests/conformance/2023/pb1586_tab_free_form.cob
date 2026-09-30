*> options: source-format=free
*> DOC-A.1-157: in free form a TAB also advances to the next tab stop, so TAB-indented
*> lines are ordinary free-form lines (kb/Work PB1586).
IDENTIFICATION DIVISION.
PROGRAM-ID. PB1586FR.
DATA DIVISION.
WORKING-STORAGE SECTION.
	01 N PIC 9 VALUE 7.
PROCEDURE DIVISION.
	DISPLAY "FREE-TAB-OK".
		IF N = 7
			DISPLAY "N7"
		END-IF.
	STOP RUN.
