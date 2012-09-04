using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Bio.IO;
using Bio.IO.FastA;
using Bio.IO.FastQ;
using SeqcosGui.Properties;
using SeqcosFilterTools.Trim;
using System.ComponentModel;

namespace SeqcosGui.Dialog
{
    /// <summary>
    /// Interaction logic for TrimDialog.xaml
    /// </summary>
    public partial class TrimDialog : ToolBaseDialog
    {
        #region Members

        /// <summary>
        /// Determines whether the trim by length tab is selected
        /// </summary>
        public override bool IsInByLengthMode
        {
            get { return this.tabByLength.IsSelected; }
        }

        /// <summary>
        /// Determines whether the trim by quality tab is selected
        /// </summary>
        public override bool IsInByQualityMode
        {
            get { return this.tabByQuality.IsSelected; }
        }

        /// <summary>
        /// Determines whether the trim by regex tab is selected
        /// </summary>
        public override bool IsInByRegexMode
        {
            get { return this.tabByRegex.IsSelected; }
        }

        /// <summary>
        /// Length of the desired trim length
        /// </summary>
        public string TrimLength
        {
            get { return trimLengthValue.Text; }
            set
            {
                if (value == trimLengthValue.Text)
                    return;
                trimLengthValue.Text = value;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for the Trim dialog
        /// </summary>
        public TrimDialog(List<string> validFileTypes) : base(validFileTypes)
        {
            InitializeComponent();

            this.ioControl.FileTypes = this.FileTypes;

            // Populate the parser type combo box
            List<string> options = GetParserTypeOptions();
            SetComboBoxOptions(this.ioControl.comboInputParserType, options);
            SetComboBoxOptions(this.ioControl.comboOutputParserType, options);

            // Populate the FASTQ type combo box
            options = GetFastqEncodingTypeOptions();
            SetComboBoxOptions(this.ioControl.comboInputFastqType, options);

            // Set the default combo index
            this.ioControl.comboInputParserType.SelectedIndex = 0;
            this.ioControl.comboOutputParserType.SelectedIndex = 0;
            this.ioControl.comboInputFastqType.SelectedIndex = 0;

            // Register IO events
            this.ioControl.DisableRunButton += new EventHandler(this.OnDisableRunBtnCalled);
            this.ioControl.EnableRunButton += new EventHandler(this.OnEnableRunBtnCalled);
        }

        /// <summary>
        /// Constructor for the Trim dialog with a supplied input filename
        /// </summary>
        /// <param name="input">Input filename</param>
        /// <param name="parser">Sequence parser</param>
        public TrimDialog(List<string> validFiles, string input, ISequenceParser parser) 
            : this(validFiles)
        {
            if (! (input == null || parser == null))
            {
                this.ioControl.InputFilename = input;
                this.ioControl.Input = new SeqcosApp.InputSubmission(input);

                this.ioControl.comboInputParserType.IsEnabled = true;

                // Update combo boxes
                this.ioControl.SelectedInputParserType = parser.Name;
                this.ioControl.SelectedOutputParserType = parser.Name;
            }
        }

        #endregion

        #region Public events

        public event EventHandler<FilterToolArgs> PrepareToTrim;

        #endregion

        #region Methods

        /// <summary>
        /// Checks if all conditions are satisfied in order for the
        /// Run button to be enabled.
        /// </summary>
        /// <returns>An error message if validation failed. Otherwise null.</returns>
        protected override string CanContinueRun()
        {
            // Trim by Length mode
            if (IsInByLengthMode)
            {
                if (Validation.GetHasError(trimLengthValue))
                    return Validation.GetErrors(trimLengthValue)[0].ErrorContent.ToString();
            }
            // Trim by Quality mode
            else if (IsInByQualityMode)
            {
                if (Validation.GetHasError(trimQualityValue))
                    return Validation.GetErrors(trimQualityValue)[0].ErrorContent.ToString();

                if (Validation.GetHasError(trimQualityMinLengthValue))
                    return Validation.GetErrors(trimQualityMinLengthValue)[0].ErrorContent.ToString();
            }
            // Trim by Regex mode
            else if (IsInByRegexMode)
            {
                if (Validation.GetHasError(trimRegexPattern))
                    return Validation.GetErrors(trimRegexPattern)[0].ErrorContent.ToString();
            }

            string ioStatus = base.VerifyIOFields(this.ioControl);
            return ioStatus;
        }

        /// <summary>
        /// This event is called by ToolBaseDialog to disable the
        /// Run button. Generally this happens when not all the 
        /// required properties in the dialog have been set.
        /// </summary>
        /// <param name="sender">ToolBaseDialog user control</param>
        /// <param name="e">Event args</param>
        private void OnDisableRunBtnCalled(object sender, EventArgs e)
        {
            base.OnDisableRunBtnCalled(this.btnRun);
        }

        /// <summary>
        /// This event is called by ToolBaseDialog to enable the
        /// Run button.
        /// </summary>
        /// <param name="sender">ToolBaseDialog user control</param>
        /// <param name="e">Event args</param>
        private void OnEnableRunBtnCalled(object sender, EventArgs e)
        {
            base.OnEnableRunBtnCalled(this.btnRun);
        }

        /// <summary>
        /// If all input parameters are satisfactory, this method is called to
        /// prepare the parameters for analysis/post-QC filtering
        /// </summary>
        /// <param name="sender">Run button element</param>
        /// <param name="e">Routed event args</param>
        protected override void PrepareRun(object sender, RoutedEventArgs e)
        {
            #region Prepare event arguments for trimming

            // Build event arguments
            bool trimFromStart = this.comboItemFromLeft.IsSelected;

            // Verify input parser
            bool canAutoParse = base.VerifyInputParser(this.ioControl.Input, this.ioControl.SelectedInputParserType);
            if (!canAutoParse)
            {
                MessageBox.Show(Resource.AUTOPARSE_FAIL);
            }
            else
            {
                // Verify output sequence formatters
                ISequenceFormatter filtered = DetermineSequenceFormatter(this.ioControl.SelectedOutputParserType, this.ioControl.OutputFilename);
                ISequenceFormatter discarded = DetermineSequenceFormatter(this.ioControl.SelectedOutputParserType, this.ioControl.DiscardedFilename);
                if (filtered == null)
                {
                    // The program shouldn't reach here
                    throw new ApplicationException(Resource.NonsenseError);
                }

                FilterToolArgs args;

                if (this.IsInByLengthMode)
                {
                    args = new TrimByLengthArgs(Convert.ToDouble(this.trimLengthValue.Text), trimFromStart, this.ioControl.Input, filtered, discarded, this.ioControl.OutputFilename);
                }
                else if (this.IsInByQualityMode)
                {
                    int minLength = this.trimQualityMinLengthValue.Text.Equals("") ? TrimByQuality.MIN_LENGTH_DEFAULT : Convert.ToInt32(this.trimQualityMinLengthValue.Text);
                    args = new TrimByQualityArgs(Convert.ToByte(this.trimQualityValue.Text), trimFromStart, minLength, this.ioControl.Input, filtered, discarded, this.ioControl.OutputFilename);
                }
                else if (this.IsInByRegexMode)
                {
                    args = new TrimByRegexArgs(this.ioControl.Input, filtered, discarded, this.trimRegexPattern.Text, this.ioControl.OutputFilename);
                }
                else
                {
                    // The program shouldn't reach here either.
                    throw new ApplicationException(Resource.NonsenseError);
                }

                this.PrepareToTrim(sender, args);

                // release files from memory
                filtered.Dispose();
                if (discarded != null) { discarded.Dispose(); }
            }
            #endregion
        }

        /// <summary>
        /// Closes this window when clicked
        /// </summary>
        /// <param name="sender">Framework element.</param>
        /// <param name="e">Routed event args</param>
        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Resets all fields in the current dialog window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnResetClicked(object sender, RoutedEventArgs e)
        {
            if (this.IsInByLengthMode)
            {
                this.trimLengthValue.Text = Convert.ToString(0);
                this.comboTrimDirection.SelectedIndex = 0;
            }
            else if (this.IsInByQualityMode)
            {
                this.trimQualityValue.Text = Convert.ToString(0);
                this.trimQualityMinLengthValue.Text = Convert.ToString(0);
            }
            else if (this.IsInByRegexMode)
            {
                this.trimRegexPattern.Text = "";
            }

            this.ioControl.Reset();
        }
        #endregion
    }
}
