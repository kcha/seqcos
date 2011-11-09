// *********************************************************************
// 
//     Copyright (c) Microsoft, 2011. All rights reserved.
//     This code is licensed under the Apache License, Version 2.0.
//     THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
//     ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
//     IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
//     PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
// 
// *********************************************************************
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using Bio.IO;
using SeqcosGui.Properties;
using System.Windows.Controls;

namespace SeqcosGui.Dialog
{
    /// <summary>
    /// Interaction logic for DiscardDialog.xaml
    /// </summary>
    public partial class DiscardDialog : ToolBaseDialog
    {
        #region Members

        /// <summary>
        /// Determines whether the discard by length tab is selected
        /// </summary>
        public override bool IsInByLengthMode
        {
            get { return this.tabByLength.IsSelected; }
        }

        /// <summary>
        /// Determines whether the discard by quality tab is selected
        /// </summary>
        public override bool IsInByQualityMode
        {
            get { return this.tabByQuality.IsSelected; }
        }

        /// <summary>
        /// Determines whether the discard by regex tab is selected
        /// </summary>
        public override bool IsInByRegexMode
        {
            get { return this.tabByRegex.IsSelected; }
        }

        #endregion

        #region Constructor


        public DiscardDialog(List<string> validFileTypes) : base(validFileTypes)
        {
            InitializeComponent();

            this.ioControl.FileTypes = this.FileTypes;

            // Populate the parser type combo box
            List<string> options = GetParserTypeOptions();
            SetComboBoxOptions(this.ioControl.comboInputParserType, options);
            SetComboBoxOptions(this.ioControl.comboOutputParserType, options);

            // Set the default combo index
            this.ioControl.comboInputParserType.SelectedIndex = 0;
            this.ioControl.comboOutputParserType.SelectedIndex = 0;

            // Register IO events
            this.ioControl.DisableRunButton += new EventHandler(this.OnDisableRunBtnCalled);
            this.ioControl.EnableRunButton += new EventHandler(this.OnEnableRunBtnCalled);

        }

        public DiscardDialog(List<string> validFiles, string input, ISequenceParser parser)
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

        public event EventHandler<FilterToolArgs> PrepareToDiscard;

        #endregion

        #region Methods

        /// <summary>
        /// Checks if all conditions are satisfied in order for the
        /// Run button to be enabled.
        /// </summary>
        /// <returns>An error message if validation failed. Otherwise null.</returns>
        protected override string CanContinueRun()
        {
            if (IsInByLengthMode)
            {
                if (Validation.GetHasError(discardLengthValue))
                {
                    return Validation.GetErrors(discardLengthValue)[0].ErrorContent.ToString();
                }

            }
            else if (IsInByQualityMode)
            {
                if (Validation.GetHasError(discardQualityValue))
                {
                    return Validation.GetErrors(discardQualityValue)[0].ErrorContent.ToString();
                }
            }
            else if (IsInByRegexMode)
            {
                if (Validation.GetHasError(discardRegexPattern))
                    return Validation.GetErrors(discardRegexPattern)[0].ErrorContent.ToString();
            }

            string ioStatus = base.VerifyIOFields(this.ioControl);
            return ioStatus;
        }

        /// <summary>
        /// This event is called by FilterToolsIODialog to disable the
        /// Run button. Generally this happens when not all the 
        /// required properties in the dialog have been set.
        /// </summary>
        /// <param name="sender">FilterToolsIODialog user control</param>
        /// <param name="e">Event args</param>
        private void OnDisableRunBtnCalled(object sender, EventArgs e)
        {
            base.OnDisableRunBtnCalled(this.btnRun);
        }

        /// <summary>
        /// This event is called by FilterToolsIODialog to enable the
        /// Run button.
        /// </summary>
        /// <param name="sender">FilterToolsIODialog user control</param>
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
            #region Prepare event arguments for discarding

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
                    args = new DiscardByLengthArgs(this.ioControl.Input, filtered, discarded, Convert.ToInt64(this.discardLengthValue.Text), this.ioControl.OutputFilename);
                }
                else if (this.IsInByQualityMode)
                {
                    args = new DiscardByMeanQualityArgs(this.ioControl.Input, filtered, discarded, Convert.ToByte(this.discardQualityValue), this.ioControl.OutputFilename);
                }
                else if (this.IsInByRegexMode)
                {
                    args = new DiscardByRegexArgs(this.ioControl.Input, filtered, discarded, this.discardRegexPattern.Text, this.ioControl.OutputFilename);
                }
                else
                {
                    // The program shouldn't reach here either.
                    throw new ApplicationException(Resource.NonsenseError);
                }

                this.PrepareToDiscard(sender, args);

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

        #endregion
    }
}
