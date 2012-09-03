#############################################
# Sequence Quality Control Studio (SeQCoS)
#############################################

SeQCoS is an open source software suite for performing quality control 
of massively parallel sequencing reads.

SeQCoS is available at http://seqcos.codeplex.com under the 
Apache License, Version 2.0 (found here http://seqcos.codeplex.com/license).

The user manual is available online: http://seqcos.codeplex.com/documentation

All bug reports and feedback are appreciated and should be reported on the 
Codeplex project website.

INSTALLATION:
-------------
First, older installations of SeQCoS must be uninstalled via the Control Panel.
Next, if applicable, install any prerequisites, including .NET 4 Framework and 
Sho. Finally, run the MSI install file and follow the on screen instructions.

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

UNINSTALLATION:
----------------
Simply uninstall from the Control Panel.



