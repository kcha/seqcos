#############################################
# Sequence Quality Control Studio (SeQCoS)
#############################################

SeQCoS is an open source software suite for performing quality control 
of massively parallel sequencing reads.

For more details, the user manual can be found under the "Docs" folder.

INSTALLATION:
-------------

 1) Older installations of SeQCoS must be uninstalled via the Control Panel.
 2) If applicable, install prerequisites, including .NET 4 Framework and 
Sho. 
 3) Run the MSI install file and follow the on screen instructions.

A copy of Bio.dll from .NET Bio ([Version 1.0](https://bio.codeplex.com/releases/view/74962), Oct 12 2011) is distributed with this installation. .NET Bio is available under the Apache License, Version 2.0.

RUNNING THE APPLICATION:
------------------------
The SeQCoS UI can be started by running SeqcosGui.exe. Alternatively, command-
line versions of SeQCoS can be used. There are three applications:

1) SeqcosUtil.exe
	This is the main application that performs quality control and generates
	plots and summary files.
	
2) SeqcosTrimmerUtil.exe - 
	Performs trimming functions on an input sequence file.
	
3) SeqcosDiscarderUtil.exe - 
	Performs discarding functions on an input sequence file.
	
Further details regarding the available options for the above command-line
applications can be found by calling the program with the option "/help"
(e.g. "SeqcosUtil.exe /help").

UNINSTALL:
----------------
Simply uninstall from the Control Panel.



