using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;

namespace ConfigUpdaterUtil
{
    /// <summary>
    /// This aplication updates the codeBase element in all *.exe.config files to the 
    /// correct Sho path.
    /// 
    /// This program is used to address the issue with SeQCoS not being able to locate
    /// Sho-related DLLs (i.e. ShoViz.dll). This is done by looking up the correct 
    /// install directory indicated stored in the environment variable SHODIR and 
    /// replacing it with the path listed in the codeBase href element.
    /// 
    /// i.e. The codeBase href element is points to "C:\Program Files\Sho 2.0 for .NET 4\...".
    /// However, if the user installed Sho in a different location, the above will not
    /// be valid and must be updated. The user can manually update the config files, or
    /// run this application. This only needs to be done once.
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("*** ConfigUpdaterUtil.exe for SeQCoS ***");
            Console.WriteLine("(This tool updates, if necessary, the Sho directory in *.exe.config files.)\n");
            Console.WriteLine("Current directory: {0}", Directory.GetCurrentDirectory());

            string shoDir = Environment.GetEnvironmentVariable("SHODIR");
 
            if (shoDir == null)
            {
                Console.Error.WriteLine("Unable to locate SHODIR environment variable. Is Sho installed?");
                Console.Error.WriteLine("Press any key to continue...");
                Console.ReadKey(true);
                Environment.Exit(0);
            }

            //string shoLibPath =  shoDir + @"bin\ShoViz.dll";
            string currentDirectory = Directory.GetCurrentDirectory();

            // Locate all the *.config files in the current directory.
            string[] configFiles = Directory.GetFiles(currentDirectory, @"*.exe.config");

            Regex pattern = new Regex(@"^\s*<codeBase.*href=""file://((.*)(bin\\.*\.dll))"".*>$");

            int numMatchedFiles = 0;
            int editedFiles = 0;

            foreach (string config in configFiles) 
            {
                Console.Write("Checking " + Path.GetFileName(config) + "...");

                StringBuilder newConfig = new StringBuilder();
                string tmp = "";
                string[] reader = File.ReadAllLines(config);

                int localEditedLines = 0;

                foreach (string line in reader)
                {
                    tmp = line;
                    // Match with regex pattern
                    MatchCollection matches = pattern.Matches(line);

                    if (matches.Count > 0)
                    {
                        numMatchedFiles++;
                        
                        foreach (Match match in matches)
                        {
                            // Extract match group (i.e. href value)
                            Group hrefElement = match.Groups[1];
                            Group shoLibraryPrefix = match.Groups[2];
                            Group shoLibrarySuffix = match.Groups[3];
                            if (hrefElement.Success && !shoLibraryPrefix.Value.Equals(shoDir))
                            {
                                // Replace value
                                tmp = line.Replace(hrefElement.Value, shoDir + shoLibrarySuffix.Value);
                                localEditedLines++;
                            }
                        }
                    }
                    
                    newConfig.AppendLine(tmp);
                }

                if (localEditedLines > 0)
                {
                    Console.Write("match(es) found, {0} line(s) updated...", localEditedLines);
                    editedFiles++;
                }

                Console.WriteLine();
                File.WriteAllText(config, newConfig.ToString());
            }

            if (editedFiles > 0)
            {
                Console.WriteLine("\n\n{0} files with Sho-related paths were found. Of these, {1} were updated.", numMatchedFiles, editedFiles);
            }
            else
            {
                Console.WriteLine("\n\nNo updates needed.");
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }

    }
}
