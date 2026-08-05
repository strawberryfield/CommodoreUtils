set C64ListExe=c:\Commodore\C64List.exe
set c64tass="C:\Commodore\64tass\64tass.exe"
set c1541="C:\Commodore\GTK3VICE-3.9-win64-r45735\bin\c1541.exe"
set prg=imgviewer

%C64ListExe% viewer.bas -loadext:lbl -d64:%prg%.d64::"VIEWER"  -ovr -crunch
%C64ListExe% mcviewer.bas -loadext:lbl -d64:%prg%.d64::"MCVIEWER"  -ovr -crunch
%C64ListExe% hrviewer.bas -loadext:lbl -d64:%prg%.d64::"HRVIEWER"  -ovr -crunch

REM Assemble loaddir.asm to loaddir.prg
%c64tass% -a loaddir.asm -o loaddir.prg

REM Add loaddir.prg to the disk image (needed by viewer.bas)
%c1541% -attach %prg%.d64 -delete "loaddir"
%c1541% -attach %prg%.d64 -write loaddir.prg "loaddir"