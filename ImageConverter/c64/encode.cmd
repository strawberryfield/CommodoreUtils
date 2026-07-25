set C64ListExe=c:\Commodore\C64List.exe
set prg=imgviewer

%C64ListExe% mcviewer.bas -loadext:lbl -d64:%prg%.d64::"MCVIEWER"  -ovr -crunch
%C64ListExe% hrviewer.bas -loadext:lbl -d64:%prg%.d64::"HRVIEWER"  -ovr -crunch