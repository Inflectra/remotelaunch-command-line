using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace Inflectra.RemoteLaunch.Engines.CommandLine
{
    /// <summary>
    /// Provides a wrapper round the simple command line automated test launcher
    /// </summary>
    public class CommandLineRunner
    {
        #region Properties

        /// <summary>
        /// Handle to the application log
        /// </summary>
        public EventLog ApplicationLog
        {
            get;
            set;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Executes the current test suite or test case and returns the results
        /// </summary>
        /// <param name="arguments">The arguments (as one concatenated string)</param>
        /// <param name="command">The command to execute, including the path of the command</param>
        /// <param name="logResults">Are we supposed to be logging results</param>
        /// <returns>The test results</returns>
        public string Execute(string command, string arguments, bool logResults)
        {

            //Get the filename and folder separately
            string commandFilename = Path.GetFileName(command);
            string commandDirectory = Path.GetDirectoryName(command);

            //Actually start the process and capture the output
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.ErrorDialog = false;

                //See if we have any environment variables to expand
                command = Environment.ExpandEnvironmentVariables(command);

                //See if we need UAC elevation
                if (Properties.Settings.Default.RunAsAdmin)
                {
                    //Can't simply redirect output, need to use a command-line pipe
                    //and actually change the command to be CMD using UseShellExecute = true

                    //So check if we actually need to log results before doing this
                    if (logResults)
                    {
                        //We store the script output in a temp file in a remote launch folder
                        string outputFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\RemoteLaunch";
                        if (!Directory.Exists(outputFolder))
                        {
                            Directory.CreateDirectory(outputFolder);
                        }
                        string outputFile = Path.Combine(outputFolder, "CommandLine_Output.log");

                        //Need to get the path to the "CMD.EXE" program
                        string cmdPath = Path.Combine(Environment.SystemDirectory, "cmd.exe");

                        startInfo.FileName = cmdPath;
                        startInfo.Arguments = "/C \"\"" + command + "\" " + arguments + " > " + outputFile + "\"";
                        startInfo.WorkingDirectory = Environment.SystemDirectory;
                        startInfo.UseShellExecute = true;
                        startInfo.Verb = "runas";
                        startInfo.RedirectStandardOutput = false;

                        //Now launch the runner and capture the output
                        try
                        {
                            Process process = new Process();
                            //Have to capture the output using pipes if running as admin
                            process.StartInfo = startInfo;
                            process.Start();
                            process.WaitForExit();
                            process.Close();

                            //Capture the output
                            string output = "";
                            if (File.Exists(outputFile))
                            {
                                StreamReader streamReader = File.OpenText(outputFile);
                                if (streamReader != null)
                                {
                                    output = streamReader.ReadToEnd();
                                    streamReader.Close();

                                    //Finally delete the output file
                                    try
                                    {
                                        File.Delete(outputFile);
                                    }
                                    catch (Exception exception)
                                    {
                                        throw new ApplicationException("Unable to delete output log file due to error - " + exception.Message, exception);
                                    }
                                }
                            }

                            //Now return the raw results
                            return output;
                        }
                        catch (ApplicationException)
                        {
                            throw;
                        }
                        catch (Exception exception)
                        {
                            throw new ApplicationException("Unable to launch Command-Line tool '" + commandFilename + "' in directory '" + commandDirectory + "' with arguments '" + arguments + "' (" + exception.Message + ")", exception);
                        }
                    }
                    else
                    {
                        //Need to get the path to the "CMD.EXE" program
                        string cmdPath = Path.Combine(Environment.SystemDirectory, "cmd.exe");

                        startInfo.FileName = cmdPath;
                        startInfo.Arguments = "/C \"\"" + command + "\" " + arguments + "\"";
                        startInfo.WorkingDirectory = Environment.SystemDirectory;
                        startInfo.UseShellExecute = true;
                        startInfo.Verb = "runas";
                        startInfo.RedirectStandardOutput = false;

                        //Now launch the runner
                        try
                        {
                            Process process = new Process();
                            //Have to capture the output using pipes if running as admin
                            process.StartInfo = startInfo;
                            process.Start();
                            process.WaitForExit();
                            process.Close();

                            //We won't have any results to send back in this case
                            return "";
                        }
                        catch (ApplicationException)
                        {
                            throw;
                        }
                        catch (Exception exception)
                        {
                            throw new ApplicationException("Unable to launch Command-Line tool '" + commandFilename + "' in directory '" + commandDirectory + "' with arguments '" + arguments + "' (" + exception.Message + ")", exception);
                        }
                    }
                }
                else
                {
                    //Just use the normal redirect process
                    startInfo.FileName = command;
                    startInfo.Arguments = arguments;
                    startInfo.WorkingDirectory = commandDirectory;
                    startInfo.UseShellExecute = false;
                    startInfo.RedirectStandardOutput = true;

                    //Now launch the runner and capture the output
                    try
                    {
                        Process process = new Process();
                        process.StartInfo = startInfo;
                        process.Start();
                        string output = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();
                        process.Close();

                        //Now return the raw results
                        return output;
                    }
                    catch (Exception exception)
                    {
                        throw new ApplicationException("Unable to launch Command-Line tool '" + commandFilename + "' in directory '" + commandDirectory + "' with arguments '" + arguments + "' (" + exception.Message + ")", exception);
                    }
                }
            }
            catch (ApplicationException exception)
            {
                throw exception;
            }
            catch (Exception exception)
            {
                throw new ApplicationException("Unable to launch Command-Line tool '" + command + "' with arguments '" + arguments + "' (" + exception.Message + ")", exception);
            }
        }

        #endregion
    }
}
