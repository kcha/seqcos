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
using System.Windows.Controls;
using SeqcosApp;
using SeqcosGui.Properties;

namespace SeqcosGui.Dialog
{
    /// <summary>
    /// Interaction logic for IoUserControl.xaml
    /// </summary>
    public partial class IoUserControl : UserControl
    {
        #region Members

        /// <summary>
        /// An instance of the InputSubmission class from the
        /// main application framework where
        /// parsing of the sequence file is handled
        /// </summary>
        public InputSubmission Input { get; set; }

        /// <summary>
        /// List of valid file types
        /// </summary>
        public List<string> FileTypes { get; set; }

        #endregion

        #region Public events

        /// <summary>
        /// Event to handle the disabling of the Run button
        /// </summary>
        public event EventHandler DisableRunButton;

        /// <summary>
        /// Event to handle the enabling of the Run button
        /// </summary>
        public event EventHandler EnableRunButton;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for creating a IO User Control
        /// </summary>
        public IoUserControl()
        {
            InitializeComponent();
        }

        #endregion Constructor

        #region Properties

        /// <summary>
        /// Gets and sets the input filename
        /// </summary>
        public string InputFilename
        {
            get { return textInputFilename.Text; }
            set { textInputFilename.Text = value; }
        }

        /// <summary>
        /// Gets or sets the output filename
        /// </summary>
        public string OutputFilename
        {
            get { return textOutputFilename.Text; }
            set { textOutputFilename.Text = value; }
        }

        /// <summary>
        /// Gets or sets the discarded filename
        /// </summary>
        public string DiscardedFilename
        {
            get { return textDiscardedFilename.Text; }
            set { textDiscardedFilename.Text = value; }
        }

        /// <summary>
        /// Gets or sets the input parser type combo box
        /// </summary>
        public string SelectedInputParserType
        {
            get { return ((ComboBoxItem)comboInputParserType.SelectedItem).Content as string; }
            set { SetComboBoxSelectedItem(comboInputParserType, value); }
        }

        /// <summary>
        /// Gets or sets the output parser type combo box
        /// </summary>
        public string SelectedOutputParserType
        {
            get { return ((ComboBoxItem)comboOutputParserType.SelectedItem).Content as string; }
            set { SetComboBoxSelectedItem(comboOutputParserType, value); }
        }

        /// <summary>
        /// Gets the selected index for input parser combo box
        /// </summary>
        public int SelectedInputParserTypeIndex
        {
            get { return comboInputParserType.SelectedIndex; }
        }

        /// <summary>
        /// Gets the selected index for the output parser combo box
        /// </summary>
        public int SelectedOutputParserTypeIndex
        {
            get { return comboOutputParserType.SelectedIndex; }
        }

        #endregion

        /// <summary>
        /// Sets the combo box selected item
        /// </summary>
        /// <param name="combo">The combo box to be set</param>
        /// <param name="formatAsString">The formatAsString to be set</param>
        private void SetComboBoxSelectedItem(ComboBox combo, string value)
        {
            foreach (ComboBoxItem item in combo.Items)
            {
                string itemParser = item.Content as string;

                if (itemParser != null)
                {
                    if (itemParser.Equals(value))
                    {
                        combo.SelectedItem = item;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// This event is raised when the user changes the combo box for
        /// choosing the input parser type.
        /// </summary>
        /// <param name="sender">Framework element</param>
        /// <param name="e">Selection changed event args</param>
        private void OnParserInputSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ToggleRunButton(this.comboInputParserType.SelectedIndex, sender, e);

            /// FASTQ combo box menu - not implemented
            //if (this.comboInputParserType.SelectedIndex == 0)
                //this.comboInputFastqType.IsEnabled = true;
            //else
                //this.comboInputFastqType.IsEnabled = false;
        }

        /// <summary>
        /// This event is raised when the user changes the combo box for
        /// choosing the output parser type.
        /// </summary>
        /// <param name="sender">Framework element</param>
        /// <param name="e">Selection changed event args</param>
        private void OnParserOutputSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ToggleRunButton(this.comboOutputParserType.SelectedIndex, sender, e);
        }

        /// <summary>
        /// Toggle the availability of the Run button depending 
        /// on the currently selected index from the combo box.
        /// </summary>
        /// <param name="selectedIndex">The selected index</param>
        private void ToggleRunButton(int selectedIndex, object sender, SelectionChangedEventArgs e)
        {
            if (selectedIndex == 0)
            {
                if (this.DisableRunButton != null)
                {
                    this.DisableRunButton(sender, e);
                }
            }
            else
            {
                if (this.EnableRunButton != null)
                {
                    this.EnableRunButton(sender, e);
                }
            }
        }

        /// <summary>
        /// Launches the OpenFileDialog, selects the file and validates it
        /// </summary>
        /// <param name="sender">Framework element.</param>
        /// <param name="e">Routed event args.</param>
        private void OnBrowseInputButtonClick(object sender, RoutedEventArgs e)
        {
            using (System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;

                // Disable selection of multiple files (will support multiselection in the future)
                dialog.Multiselect = false;

                dialog.Filter = String.Join("|", this.FileTypes.ToArray());
                dialog.FilterIndex = 2;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string autoParser = null;
                    bool chooseParserManually = false;

                    this.Input = new InputSubmission(dialog.FileNames[0]);

                    if (Input.CanParseInputByFileName())
                    {
                        autoParser = Input.Parser.Name;
                    }
                    else
                    {
                        chooseParserManually = true;
                    }

                    // Display filename in text box
                    this.InputFilename = dialog.FileNames[0];
                    this.comboInputParserType.IsEnabled = true;

                    // Enable parser selection if applicable
                    if (chooseParserManually)
                    {
                        this.comboInputParserType.SelectedIndex = 0;

                        System.Windows.MessageBox.Show(Resource.AUTOPARSE_FAIL);
                    }
                    else
                    {
                        this.SelectedInputParserType = Input.Parser.Name;
                        this.SelectedOutputParserType = Input.Parser.Name;
                        this.btnBrowseForOutput.Focus();
                    }
                }
            }
        }

        /// <summary>
        /// Launches the OpenFileDialog and allows user to select an
        /// output file for filtered reads.
        /// </summary>
        /// <param name="sender">Framework element</param>
        /// <param name="e">Routed event args</param>
        private void OnBrowseOutputButtonClick(object sender, RoutedEventArgs e)
        {
            string file = GetSavedFilenameFromOpenFileDialog();
            this.OutputFilename = file;
            this.comboOutputParserType.IsEnabled = true;
            this.textDiscardedFilename.IsEnabled = true;
            this.btnBrowseForDiscarded.IsEnabled = true;
        }

        /// <summary>
        /// Launches the OpenFileDialog and allows user to select
        /// an output file for discarded reads.
        /// </summary>
        /// <param name="sender">Framework element</param>
        /// <param name="e">Routed event args</param>
        private void OnBrowseDiscardButtonClick(object sender, RoutedEventArgs e)
        {
            string file = GetSavedFilenameFromOpenFileDialog();
            this.DiscardedFilename = file;
        }

        /// <summary>
        /// Launches OpenFileDialog and allows the user to select an output file
        /// </summary>
        /// <returns>The selected filename</returns>
        private string GetSavedFilenameFromOpenFileDialog()
        {
            string filename = null;

            using (System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    dialog.Filter = String.Join("|", this.FileTypes.ToArray());

                    switch (this.SelectedOutputParserTypeIndex)
                    {
                        // FastQ
                        case 1:
                            dialog.FilterIndex = 1;
                            break;

                        // FastA
                        case 2:
                            dialog.FilterIndex = 2;
                            break;

                        default:
                            dialog.FilterIndex = 0;
                            break;
                    }

                    dialog.CheckFileExists = false;
                    dialog.CheckPathExists = true;
                    dialog.OverwritePrompt = true;
                
                    filename = dialog.FileNames[0];
                }
            }

            return filename;
        }
    }
}
