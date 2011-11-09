// *********************************************************************
// 
//     Copyright (c) Microsoft, 2011. All rights reserved.
//     This code is licensed under the Apache License, Version 2.0.
//     THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//     ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//     IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//     PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
// 
// *******************************************************************
using System;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace SeqcosApp.Analyzer.NCBI
{
    /// <summary>
    /// This class defines the environment for executing local NCBI BLAST service. It includes
    /// performing pre-BLAST functions such as downloading sequence files from FTP 
    /// and formatting it into a BLAST database.
    /// </summary>
    public class BlastLocalHandler
    {
        #region Members
        public string BlastDbPath { get; private set; }
        public static string BlastVersion = Properties.Resource.BLAST_VERSION;
        public readonly string BlastExe;
        public string QueryFilename { get; private set; }
        public string OutputFilename { get; private set; }
        public string DbFilename { get; private set; }

        #endregion

        /// <summary>
        /// Constructor for running BLAST services.
        /// </summary>
        /// <param name="exe">The name of the BLAST service to be run.</param>
        /// <param name="dbPath">Path of the blastdb directory.</param>
        public BlastLocalHandler(string exe = null, string dbPath = null)
        {
            // Get the blastdb environtment variable, if it was not specified by the user.
            if (dbPath == null)
            {
                this.BlastDbPath = Environment.GetEnvironmentVariable(Properties.Resource.BlastDbEnvironmentVariable);

                if (this.BlastDbPath == null)
                {
                    string message = string.Format("Unable to find the environment variable {0}. Please make sure it is correctly set up and try again.", Properties.Resource.BlastDbEnvironmentVariable);
                    throw new ArgumentNullException(Properties.Resource.BlastDbEnvironmentVariable, message);
                }
            }
            else if (!Directory.Exists(dbPath))
            {
                throw new FileNotFoundException(Properties.Resource.FileNotFound);
            }

            this.BlastExe = exe == null ? Properties.Resource.BLASTN : exe;
        }

        /// <summary>
        /// Execute a local BLAST 
        /// </summary>
        /// <param name="query">The query file in FASTA format</param>
        /// <param name="output">The name of the BLAST output file</param>
        public void ExecuteLocal(string query, string output, IBlastParameters args)
        {
            // Validate parameters
            string commandArguments = BlastTools.FormatBlastArguments(query, output, args);

            // Start the process
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = this.BlastExe,
                Arguments = commandArguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using (Process p = Process.Start(psi))
            {
                using (StreamReader reader = p.StandardOutput)
                {
                    string resultStdOut = reader.ReadToEnd();
                    Console.Write(resultStdOut);
                }

                using (StreamReader error = p.StandardError)
                {
                    string resultStdErr = error.ReadToEnd();
                    Console.Error.Write(resultStdErr);
                }
            }

            this.QueryFilename = query;
            this.DbFilename = args.Database;
            this.OutputFilename = output;
        }

        /// <summary>
        /// Download FASTA file from FTP
        /// </summary>
        /// <param name="ftpUrl">FTP address</param>
        /// <param name="filename">Name of the file to be downloaded</param>
        /// <param name="outputDir">Local directory where file will be saved in</param>
        /// <param name="overwrite">If the requested file already exists locally, it will be overwritten if true. False by default</param>
        /// <returns>Status description from FtpWebRequest</returns>
        static public string DownloadFasta(string ftpUrl, string filename, string outputDir, bool overwrite = false)
        {
            if (ftpUrl == null)
                throw new ArgumentNullException("ftpUrl");

            if (filename == null)
                throw new ArgumentNullException("filename");

            string localPath = outputDir + "\\" + filename;
            if (File.Exists(localPath) && !overwrite)
                return "incomplete. File already exists.";

            string ftpPath = ftpUrl + "/" + filename;
            var req = (FtpWebRequest)WebRequest.Create(ftpPath);
            req.Method = WebRequestMethods.Ftp.DownloadFile;

            using (FtpWebResponse resp = (FtpWebResponse)req.GetResponse())
            {
                Stream responseStream = resp.GetResponseStream();
                FileStream writeStream = new FileStream(localPath, FileMode.Create);

                int length = 2048;
                byte[] buffer = new byte[length];
                int bytesRead = responseStream.Read(buffer, 0, length);
                while (bytesRead > 0)
                {
                    writeStream.Write(buffer, 0, bytesRead);
                    bytesRead = responseStream.Read(buffer, 0, length);
                }
                writeStream.Close();

                //Console.WriteLine("Download Complete, status {0}", resp.StatusDescription);
                return resp.StatusDescription;
            }
        }

        /// <summary>
        /// Format a FASTA file to a BLAST database
        /// </summary>
        /// <param name="fastaFile">Input FASTA file</param>
        /// <param name="blastDbPath">Path of BLAST db directory</param>
        /// <param name="molecularType">Molecular type of input</param>
        /// <param name="resultStdOut">Store standard output text from the process</param>
        /// <param name="resultStdErr">Store standard error text from the process</param>
        static public void MakeBlastDb(string fastaFile, string blastDbPath, string molecularType, out string resultStdOut, out string resultStdErr)
        {
            if (!File.Exists(fastaFile))
                throw new ArgumentNullException("fastaFile");

            // Molecular type can only be 'nucl' or 'prot'
            if (!(molecularType == "nucl" || molecularType == "prot"))
            {
                throw new ArgumentException("Invalid BLAST argument: molecular type " + molecularType
                                                + " not recognized");
            }

            // Set up parameters
            string dbName = Path.GetFileNameWithoutExtension(fastaFile);
            string exe = "makeblastdb";
            string commandArguments = "-in " + BlastTools.InDoubleQuotes(dbName)
                                    + " -out " + BlastTools.InDoubleQuotes(dbName)
                                    + " -dbtype " + molecularType
                                   ; 

            // Temporarily change working directory to blastdb.
            // This is because makeblastdb wasn't playing nice with
            // arguments that have spaces within.
            var oldWorkingDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(blastDbPath);

            // Start the process
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = commandArguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using (Process p = Process.Start(psi))
            {
                using (StreamReader reader = p.StandardOutput)
                {
                    resultStdOut = reader.ReadToEnd();
                    //Console.Write(result);
                }

                using (StreamReader error = p.StandardError)
                {
                    resultStdErr = error.ReadToEnd();
                    //Console.Error(resultError);
                }
            }

            // Change back to previous directory
            Directory.SetCurrentDirectory(oldWorkingDirectory);
        }

        #region Private Methods

        /// <summary>
        /// Check if BLAST is installed on the local machine.
        /// </summary>
        /// <returns></returns>
        static public bool IsLocalBLASTInstalled(string blastVersion)
        {
            // To check if BLAST is installed, search for the occurrence of the blast
            // executable in the PATH environmental variable.
            string path = Environment.GetEnvironmentVariable("path");
            return path.Contains(blastVersion);
        }



        #endregion
    }
}
