// *********************************************************************
// 
//     Copyright (c) 2011 Microsoft. All rights reserved.
//     This code is licensed under the Apache License, Version 2.0.
//     THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//     ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//     IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//     PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
// 
// *********************************************************************
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Bio.IO;
using Bio.IO.FastA;
using Bio.IO.FastQ;
using SeqcosGui.Properties;
using SeqcosApp;

namespace SeqcosGui.Dialog
{
    /// <summary>
    /// A base class derived from Window that holds common properties
    /// and methods for post-QC filtering that are shared by dialogs 
    /// that derived from this class.
    /// </summary>
    public abstract partial class ToolBaseDialog : Window
    {
        #region Members

        /// <summary>
        /// List of valid file types
        /// </summary>
        public List<string> FileTypes;

        /// <summary>
        /// Determines whether the By Length tab is selected
        /// </summary>
        abstract public bool IsInByLengthMode { get; }

        /// <summary>
        /// Determines whether the By Quality tab is selected
        /// </summary>
        abstract public bool IsInByQualityMode { get; }

        /// <summary>
        /// Determines whether the By Regex tab is selected
        /// </summary>
        abstract public bool IsInByRegexMode { get; }

        /// <summary>
        /// A set of status error messages
        /// </summary>
        protected enum RunStatus
        {
            OK,
            NoInput,
            NoOutput,
            NoValue,
            NoInputParserType,
            NoOutputParserType,
            NotNumeric,
            NotPositiveInteger,
            SameFilename
        };

        #endregion

        #region Constructor

        /// <summary>
        /// Base constructor for all post-QC filtering dialogs
        /// </summary>
        /// <param name="validFileTypes"></param>
        public ToolBaseDialog(List<string> validFileTypes) : this()
        {
            //InitializeComponent();
            this.FileTypes = validFileTypes;
        }

        public ToolBaseDialog()
        {
        }

        #endregion

        #region Abstract methods

        /// <summary>
        /// Checks if all conditions are satisfied in order for the
        /// Run button to be enabled.
        /// </summary>
        /// <returns>An error message if validation failed. Otherwise null.</returns>
        protected abstract string CanContinueRun();

        /// <summary>
        /// If all input parameters are satisfactory, this method is called to
        /// prepare the parameters for analysis/post-QC filtering
        /// </summary>
        /// <param name="sender">Run button element</param>
        /// <param name="e">Routed event args</param>
        protected abstract void PrepareRun(object sender, RoutedEventArgs e);

        /// <summary>
        /// Reset all fields to the initial settings
        /// </summary>
        /// <param name="sender">Reset button element</param>
        /// <param name="e">Routed event args</param>
        protected abstract void OnResetClicked(object sender, RoutedEventArgs e);

        #endregion

        #region Methods

        /// <summary>
        /// Returns a list of all valid combo box options for parser type
        /// </summary>
        /// <returns>List of combo box options</returns>
        protected List<string> GetParserTypeOptions()
        {
            List<string> options = new List<string>();
            options.Add(Resource.MANUAL_CHOICE);
            options.Add(SequenceParsers.FastQ.Name);
            options.Add(SequenceParsers.Fasta.Name);
            return options;
        }

        /// <summary>
        /// Returns a list of all valid combo box options for FASTQ encoding format
        /// </summary>
        /// <returns>List of combo box options</returns>
        protected List<string> GetFastqEncodingTypeOptions()
        {
            List<string> options = new List<string>();
            options.Add(Resource.MANUAL_CHOICE);

            string[] validFormats = BioHelper.QueryValidFastqFormats();
            foreach (var format in validFormats)
            {
                options.Add(format);
            }
            return options;
        }

        /// <summary>
        /// Set the options of a combo box
        /// </summary>
        /// <param name="comboBox">The combo box to be set</param>
        /// <param name="options">The options</param>
        protected void SetComboBoxOptions(ComboBox comboBox, List<string> options)
        {
            foreach (var parserName in options)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = parserName;
                item.Tag = parserName;
                comboBox.Items.Add(item);
            }
        }

        /// <summary>
        /// This event is called by ToolBaseDialog to disable the
        /// Run button. Generally this happens when not all the 
        /// required properties in the dialog have been set.
        /// </summary>
        /// <param name="btn">Button object</param>
        protected void OnDisableRunBtnCalled(Button btn)
        {
            if (btn.IsEnabled)
                btn.IsEnabled = false;
        }

        /// <summary>
        /// This event is called by ToolBaseDialog to enable the
        /// Run button.
        /// </summary>
        /// <param name="btn">Button object</param>
        protected void OnEnableRunBtnCalled(Button btn)
        {
            // Check if all conditions are satisfied for the button
            // to be enabled
            if (!btn.IsEnabled)
                btn.IsEnabled = true;
        }

        /// <summary>
        /// Checks if the given string is a numeric formatAsString.
        /// </summary>
        /// <param name="formatAsString">String formatAsString</param>
        /// <returns>True if string is numeric. Otherwise false.</returns>
        protected bool IsNumeric(string value)
        {
            Regex numericPattern = new Regex("^-?[0-9.]+$");
            return numericPattern.IsMatch(value);
        }

        /// <summary>
        /// Checks if the given string is a positive integer formatAsString
        /// </summary>
        /// <param name="formatAsString">String formatAsString</param>
        /// <returns>True if string is an integer. Otherwise false.</returns>
        protected bool IsPositiveInteger(string value)
        {
            Regex integerPattern = new Regex("^[0-9]+$");
            return integerPattern.IsMatch(value);
        }

        /// <summary>
        /// Create a SequenceFormatter object based on the given format type.
        /// </summary>
        /// <param name="parserName"></param>
        /// <param name="outputFilename"></param>
        /// <returns></returns>
        internal ISequenceFormatter DetermineSequenceFormatter(string parserName, string outputFilename)
        {
            ISequenceFormatter formatter = null;

            if (outputFilename.Equals(""))
            {
                return null;
            }
            if (parserName.Equals(SequenceFormatters.Fasta.Name))
            {
                formatter = new FastAFormatter(outputFilename);
            }
            else if (parserName.Equals(SequenceFormatters.FastQ.Name))
            {
                formatter = new FastQFormatter(outputFilename);
            }

            return formatter;
        }

        /// <summary>
        /// Verifies the IO fields to determine whether it is ready
        /// for analysis.
        /// </summary>
        /// <param name="io">IoUserControl object</param>
        /// <returns>An error message if validation failed.</returns>
        protected string VerifyIOFields(IoUserControl io)
        {
            if (io.InputFilename.Equals(""))
                return Resource.NoInputFile;

            if (io.OutputFilename.Equals(""))
                return Resource.NoOutputFile;

            // input, output and discarded filenames can't be the same as each other
            if (io.InputFilename.Equals(io.OutputFilename))
                return Resource.SameFilename;

            if (!io.DiscardedFilename.Equals("") &&
                (io.DiscardedFilename.Equals(io.OutputFilename) ||
                    io.DiscardedFilename.Equals(io.InputFilename)))
                return Resource.SameFilename;

            if (io.SelectedInputParserTypeIndex == 0)
                return Resource.NoInputParserType;

            if (io.SelectedOutputParserTypeIndex == 0)
                return Resource.NoOutputParserType;

            return null;
        }

        /// <summary>
        /// This event is raised when the user clicks on the Run button,
        /// signifying the start of the analysis.
        /// </summary>
        /// <param name="sender">Framework element</param>
        /// <param name="e">Routed event args</param>
        protected void OnRunClicked(object sender, RoutedEventArgs e)
        {
            string status = CanContinueRun();

            if (status != null)
            {
                MessageBox.Show(status);
            }
            else
            {
                PrepareRun(sender, e);
            }
        }

        /// <summary>
        /// Verifies whether the input parser is supported
        /// </summary>
        /// <param name="inputInfo">InputSubmission object</param>
        /// <param name="parserType">The parser type string</param>
        /// <returns>True if supported.</returns>
        internal bool VerifyInputParser(SeqcosApp.InputSubmission inputInfo, string parserType)
        {
            if (inputInfo.Parser == null)
            {
                if (!inputInfo.CanParseInputByName(parserType))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion


    }
}
