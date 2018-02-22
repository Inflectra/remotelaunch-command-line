using System;
using System.Collections.Generic;
using System.Text;

using Inflectra.RemoteLaunch.Interfaces;
using Inflectra.RemoteLaunch.Interfaces.DataObjects;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Inflectra.RemoteLaunch.Engines.CommandLine
{
    /// <summary>
    /// Implements the IAutomationEngine class for launching command line applications
    /// This class is instantiated by the RemoteLaunch application
    /// </summary>
    /// <remarks>
    /// The AutomationEngine class provides some of the generic functionality
    /// </remarks>
    public class CommandLineEngine : AutomationEngine, IAutomationEngine4
    {
        private const string CLASS_NAME = "CommandLineEngine";

        private const string AUTOMATION_ENGINE_TOKEN = "CommandLine";
        private const string AUTOMATION_ENGINE_VERSION = "4.0.5";

        private bool TRACE_LOGGING_ENABLED = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public CommandLineEngine()
        {
            //Set status to OK
            base.status = EngineStatus.OK;
        }

        /// <summary>
        /// Returns the author of the test automation engine
        /// </summary>
        public override string ExtensionAuthor
        {
            get
            {
                return "Inflectra Corporation";
            }
        }

        /// <summary>
        /// The unique GUID that defines this automation engine
        /// </summary>
        public override Guid ExtensionID
        {
            get
            {
                return new Guid("{95589EBC-E859-459C-904B-6524870DA4BE}");
            }
        }

        /// <summary>
        /// Returns the display name of the automation engine
        /// </summary>
        public override string ExtensionName
        {
            get
            {
                return "Command-Line Automation Engine";
            }
        }

        /// <summary>
        /// Returns the unique token that identifies this automation engine to SpiraTest
        /// </summary>
        public override string ExtensionToken
        {
            get
            {
                return AUTOMATION_ENGINE_TOKEN;
            }
        }

        /// <summary>
        /// Returns the version number of this extension
        /// </summary>
        public override string ExtensionVersion
        {
            get
            {
                return AUTOMATION_ENGINE_VERSION;
            }
        }

        /// <summary>
        /// Adds a custom settings panel for allowing the user to set any engine-specific configuration values
        /// </summary>
        /// <remarks>
        /// 1) If you don't have any engine-specific settings, just comment out the entire Property
        /// 2) The SettingPanel needs to be implemented as a WPF XAML UserControl
        /// </remarks>
        public override System.Windows.UIElement SettingsPanel
        {
            get
            {
                return new AutomationEngineSettings();
            }
            set
            {
                AutomationEngineSettings settingsPanel = (AutomationEngineSettings)value;
                settingsPanel.SaveSettings();
            }
        }

        public override AutomatedTestRun StartExecution(AutomatedTestRun automatedTestRun)
        {
            //Not used since we implement the V4 API instead
            throw new NotImplementedException();
        }

        /// <summary>
        /// This is the main method that is used to start automated test execution
        /// </summary>
        /// <param name="projectId">The id of the project</param>
        /// <param name="automatedTestRun">The automated test run object</param>
        /// <returns>Either the populated test run or an exception</returns>
        public AutomatedTestRun4 StartExecution(AutomatedTestRun4 automatedTestRun, int projectId)
        {
            //Set status to OK
            base.status = EngineStatus.OK;

            try
            {
                //Instantiate the command-line runner wrapper class
                CommandLineRunner commandLineRunner = new CommandLineRunner();

                if (TRACE_LOGGING_ENABLED)
                {
                    LogEvent("Starting test execution", EventLogEntryType.Information);
                }

                //Pass the application log handle
                commandLineRunner.ApplicationLog = this.applicationLog;

                //See if we have an attached or linked test script
                //The parameters work differently for the two types
                string arguments = "";
                string path = "";
                string testName = "";
                if (automatedTestRun.Type == AutomatedTestRun4.AttachmentType.URL)
                {
                    /*
                     * The "URL" of the test case is the full command line including the name of the application to run
                     * with the test script to execute being one of the arguments
                     * If they want to provide any arguments and parameters, they need to specify them separated by a pipe (|)
                     * (i.e. Execution Path|[Arguments]|[Parameter Mask])
                     * 
                     * e.g. [ProgramFiles]\MyCommand.exe|-execute MyScript.txt -arg1 -arg2|-name:value                    
                     * 
                     * would become:
                     * 
                     * C:\Program Files\MyCommand.exe -execute MyScript.txt -arg1 -arg2 -param1:value1 - param2:value2
                     * 
                     * If you specify [ProjectId], [TestCaseId], [TestRunId], [TestSetId] or [ReleaseId] in the list of parameters
                     * they will be replaced by the appropriate ID (if a value is set)
                     * 
                    */

                    //See if we have any pipes in the 'filename' that contains arguments or parameters
                    string[] filenameElements = automatedTestRun.FilenameOrUrl.Split('|');
                    testName = filenameElements[0];
                    if (testName.Length > 50)
                    {
                        testName = testName.Substring(0,50);
                    }

                    //To make it easier, we have certain shortcuts that can be used in the path
                    path = filenameElements[0];
                    path = path.Replace("[MyDocuments]", Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments));
                    path = path.Replace("[CommonDocuments]", Environment.GetFolderPath(System.Environment.SpecialFolder.CommonDocuments));
                    path = path.Replace("[DesktopDirectory]", Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory));
                    path = path.Replace("[ProgramFiles]", Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles));
                    path = path.Replace("[ProgramFilesX86]", Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86));

                    //See if we have any arguments (not parameters)
                    if (filenameElements.Length > 1)
                    {
                        arguments = filenameElements[1];
                        //Replace any special folders in the arguments as well
                        arguments = arguments.Replace("[MyDocuments]", Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments));
                        arguments = arguments.Replace("[CommonDocuments]", Environment.GetFolderPath(System.Environment.SpecialFolder.CommonDocuments));
                        arguments = arguments.Replace("[DesktopDirectory]", Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory));
                        arguments = arguments.Replace("[ProgramFiles]", Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles));
                        arguments = arguments.Replace("[ProgramFilesX86]", Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86));

                        //Replace the special test case and test run id tokens
                        arguments = arguments.Replace("[TestCaseId]", automatedTestRun.TestCaseId.ToString());
                        arguments = arguments.Replace("[TestRunId]", automatedTestRun.TestRunId.ToString());
                        arguments = arguments.Replace("[ProjectId]", projectId.ToString());
                        if (automatedTestRun.TestSetId.HasValue)
                        {
                            arguments = arguments.Replace("[TestSetId]", automatedTestRun.TestSetId.Value.ToString());
                        }
                        if (automatedTestRun.ReleaseId.HasValue)
                        {
                            arguments = arguments.Replace("[ReleaseId]", automatedTestRun.ReleaseId.Value.ToString());
                        }
                    }

                    //See if we have a parameter mask
                    if (filenameElements.Length > 2)
                    {
                        string parameterMask = filenameElements[2];

                        //Now iterate through the provided parameters and add to the arguments based on the mask
                        if (automatedTestRun.Parameters != null)
                        {
                            foreach (TestRunParameter parameter in automatedTestRun.Parameters)
                            {
                                string parameterArgument = parameterMask.Replace("name", parameter.Name);
                                parameterArgument = parameterArgument.Replace("value", parameter.Value);
                                arguments += " " + parameterArgument;
                            }
                        }
                    }
                }
                else
                {
                    //We have an embedded script which we need to save onto the file system so that it can be executed
                    //by the command-line tool

                    /*
                     * The filename of the test case is the full path of the application to run
                     * with the test script to execute being one of the arguments, specified as {filename}
                     * If they want to provide any arguments, they need to specify them separated by a pipe (|)
                     * (i.e. Execution Path|[Arguments])
                     * 
                     * e.g. [ProgramFiles]\MyCommand.exe|-execute "{filename}" -arg1 -arg2                   
                     * 
                     * would become:
                     * 
                     * C:\Program Files\MyCommand.exe -execute "C:\Documents And Settings\Username\Local Settings\Application Data\MyScript.txt" -arg1 -arg2
                     * 
                     * In this mode, the parameters are used to replace tokens in the actual test script rather than
                     * being passed to the command-line handler
                     * 
                     * If you specify [ProjectId], [TestCaseId], [TestRunId], [TestSetId] or [ReleaseId] in the list of parameters
                     * they will be replaced by the appropriate ID (if a value is set)
                     * 
                    */

                    //We store the script in a temp file in a remote launch folder
                    string outputFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\RemoteLaunch";
                    if (!Directory.Exists(outputFolder))
                    {
                        Directory.CreateDirectory(outputFolder);
                    }

                    //First we need to get the test script
                    if (automatedTestRun.TestScript == null || automatedTestRun.TestScript.Length == 0)
                    {
                        throw new ApplicationException("The provided test script is empty, aborting test execution");
                    }
                    string testScript = Encoding.UTF8.GetString(automatedTestRun.TestScript);

                    //Replace any parameters (in ${parametername} lower-case syntax)
                    if (automatedTestRun.Parameters != null)
                    {
                        foreach (TestRunParameter parameter in automatedTestRun.Parameters)
                        {
                            testScript = testScript.Replace(CreateParameterToken(parameter.Name), parameter.Value);
                        }
                    }

                    //Replace the special test case and test run id tokens
                    testScript = testScript.Replace("[TestCaseId]", automatedTestRun.TestCaseId.ToString());
                    testScript = testScript.Replace("[TestRunId]", automatedTestRun.TestRunId.ToString());
                    testScript = testScript.Replace("[ProjectId]", projectId.ToString());
                    if (automatedTestRun.TestSetId.HasValue)
                    {
                        testScript = testScript.Replace("[TestSetId]", automatedTestRun.TestSetId.Value.ToString());
                    }
                    if (automatedTestRun.ReleaseId.HasValue)
                    {
                        testScript = testScript.Replace("[ReleaseId]", automatedTestRun.ReleaseId.Value.ToString());
                    }

                    //Now we need to put the test script into this folder
                    string testScriptPath = outputFolder + @"\CommandLineEngine.txt";
                    StreamWriter streamWriter = new StreamWriter(testScriptPath);
                    streamWriter.Write(testScript);
                    streamWriter.Flush();
                    streamWriter.Close();

                    //See if we have any pipes in the 'filename' that contains arguments or parameters
                    string[] filenameElements = automatedTestRun.FilenameOrUrl.Split('|');
                    testName = filenameElements[0];
                    if (testName.Length > 50)
                    {
                        testName = testName.Substring(0,50);
                    }

                    //To make it easier, we have certain shortcuts that can be used in the path
                    path = filenameElements[0];
                    path = path.Replace("[MyDocuments]", Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments));
                    path = path.Replace("[CommonDocuments]", Environment.GetFolderPath(System.Environment.SpecialFolder.CommonDocuments));
                    path = path.Replace("[DesktopDirectory]", Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory));
                    path = path.Replace("[ProgramFiles]", Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles));
                    path = path.Replace("[ProgramFilesX86]", Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86));

                    //See if we have any arguments
                    if (filenameElements.Length > 1)
                    {
                        arguments = filenameElements[1];
                        //Replace any special folders in the arguments as well
                        arguments = arguments.Replace("[MyDocuments]", Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments));
                        arguments = arguments.Replace("[CommonDocuments]", Environment.GetFolderPath(System.Environment.SpecialFolder.CommonDocuments));
                        arguments = arguments.Replace("[DesktopDirectory]", Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory));
                        arguments = arguments.Replace("[ProgramFiles]", Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles));
                        arguments = arguments.Replace("[ProgramFilesX86]", Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86));
                        arguments = arguments.Replace("{filename}", testScriptPath);
                        arguments = arguments.Replace("[filename]", testScriptPath);

                        //Replace the special test case and test run id tokens
                        arguments = arguments.Replace("[TestCaseId]", automatedTestRun.TestCaseId.ToString());
                        arguments = arguments.Replace("[TestRunId]", automatedTestRun.TestRunId.ToString());
                        arguments = arguments.Replace("[ProjectId]", projectId.ToString());
                        if (automatedTestRun.TestSetId.HasValue)
                        {
                            arguments = arguments.Replace("[TestSetId]", automatedTestRun.TestSetId.Value.ToString());
                        }
                        if (automatedTestRun.ReleaseId.HasValue)
                        {
                            arguments = arguments.Replace("[ReleaseId]", automatedTestRun.ReleaseId.Value.ToString());
                        }
                    }
                }

                //Actually run the command-line test
                DateTime startDate = DateTime.Now;
                string results = commandLineRunner.Execute(path, arguments, Properties.Settings.Default.LogResults);
                DateTime endDate = DateTime.Now;

                //See if we want to log the results or not
                //This is useful when launching a process that knows how to send results back to SpiraTest itself
                //(e.g. a NUnit test suite)

                if (Properties.Settings.Default.LogResults)
                {
                    //Now extract the test results and populate the test run object
                    if (String.IsNullOrEmpty(automatedTestRun.RunnerName))
                    {
                        automatedTestRun.RunnerName = this.ExtensionName;
                    }

                    //Specify the start/end dates
                    automatedTestRun.StartDate = startDate;
                    automatedTestRun.EndDate = endDate;

                    //Put the filename as the 'test name'
                    automatedTestRun.RunnerTestName = testName;

                    //We use the Regexes to determine the status
                    automatedTestRun.ExecutionStatus = (AutomatedTestRun4.TestStatusEnum)Properties.Settings.Default.DefaultStatus;
                    Regex passRegex = new Regex(Properties.Settings.Default.PassRegex, RegexOptions.IgnoreCase);
                    Regex failRegex = new Regex(Properties.Settings.Default.FailRegex, RegexOptions.IgnoreCase);
                    Regex cautionRegex = new Regex(Properties.Settings.Default.CautionRegex, RegexOptions.IgnoreCase);
                    Regex blockedRegex = new Regex(Properties.Settings.Default.BlockedRegex, RegexOptions.IgnoreCase);

                    //Check passed
                    if (passRegex.IsMatch(results))
                    {
                        automatedTestRun.ExecutionStatus = AutomatedTestRun4.TestStatusEnum.Passed;
                    }
                    if (cautionRegex.IsMatch(results))
                    {
                        automatedTestRun.ExecutionStatus = AutomatedTestRun4.TestStatusEnum.Caution;
                    }
                    if (failRegex.IsMatch(results))
                    {
                        automatedTestRun.ExecutionStatus = AutomatedTestRun4.TestStatusEnum.Failed;
                    }
                    if (blockedRegex.IsMatch(results))
                    {
                        automatedTestRun.ExecutionStatus = AutomatedTestRun4.TestStatusEnum.Blocked;
                    }

                    if (results.Length > 50)
                    {
                        automatedTestRun.RunnerMessage = results.Substring(0, 50);
                    }
                    else
                    {
                        automatedTestRun.RunnerMessage = results;
                    }
                    automatedTestRun.Format = AutomatedTestRun4.TestRunFormat.PlainText;
                    automatedTestRun.RunnerStackTrace = results;
                    automatedTestRun.RunnerAssertCount = (automatedTestRun.ExecutionStatus == AutomatedTestRun4.TestStatusEnum.Passed) ? 0 : 1;
                }
                else
                {
                    automatedTestRun.ExecutionStatus = AutomatedTestRun4.TestStatusEnum.NotRun;
                }

                //Report as complete               
                base.status = EngineStatus.OK;
                return automatedTestRun;
            }
            catch (Exception exception)
            {
                //Log the error and denote failure
                LogEvent(exception.Message + " (" + exception.StackTrace + ")", EventLogEntryType.Error);

                //Report as completed with error
                base.status = EngineStatus.Error;
                throw exception;
            }
        }

        /// <summary>
        /// Returns the full token of a test caseparameter from its name
        /// </summary>
        /// <param name="parameterName">The name of the parameter</param>
        /// <returns>The tokenized representation of the parameter used for search/replace</returns>
        /// <remarks>We use the same parameter format as Ant/NAnt</remarks>
        public static string CreateParameterToken(string parameterName)
        {
            return "${" + parameterName + "}";
        }
    }
}
