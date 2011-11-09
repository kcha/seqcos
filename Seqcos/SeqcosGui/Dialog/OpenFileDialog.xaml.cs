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
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Bio.IO;
using SeqcosApp;
using SeqcosApp.Analyzer.NCBI;
using SeqcosGui.Properties;
using Bio.IO.FastQ;

namespace SeqcosGui.Dialog
{
    /// <summary>
    /// Interaction logic for OpenFileDialog.xaml
    /// </summary>
    public partial class OpenFileDialog : Window
    {
        #region Private members

        /// <summary>
        /// List of supported input file types
        /// </summary>
        private List<string> fileTypes;

        /// <summary>
        /// An instance of the InputSubmission class from the
        /// main application framework where
        /// parsing of the sequence file is handled
        /// </summary>
        private InputSubmission input;

        /// <summary>
        /// Set of BLAST parameters used for executing BLAST
        /// </summary>
        private IBlastParameters blastArgs;

        private List<string> availableBlastDatabases;

        #endregion

        #region Public Events

        /// <summary>
        /// Event to indicate that the analysis of the
        /// input sequence file has to prepare to run by
        /// launching the RunProgressDialog.
        /// </summary>
        public event EventHandler<OpenFileArgs> PrepareRun;

        #endregion

        /// <summary>
        /// Constructor for OpenFileDialog
        /// </summary>
        /// <param name="fileTypes">List of valid file types</param>
        public OpenFileDialog(List<string> fileTypes)
        {
            InitializeComponent();
            this.btnBrowse.Focus();

            #region Parser type combo box

            // Populate the parser type combo box
            ComboBoxItem defaultItem = new ComboBoxItem();
            defaultItem.Content = Resource.MANUAL_CHOICE;
            this.comboParserType.Items.Add(defaultItem);

            Collection<string> validParsers = new Collection<string>();
            validParsers.Add(SequenceParsers.FastQ.Name);
            validParsers.Add(SequenceParsers.Fasta.Name);

            foreach (var parserName in validParsers)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = parserName;
                item.Tag = parserName;
                this.comboParserType.Items.Add(item);
            }
            this.comboParserType.SelectedIndex = 0;

            this.fileTypes = fileTypes;

            #endregion

            #region FASTQ format combo box
            defaultItem = new ComboBoxItem();
            defaultItem.Content = Resource.MANUAL_CHOICE;
            this.comboFastqType.Items.Add(defaultItem);

            Array validFormats = BioHelper.QueryValidFastqFormats();
            foreach (var format in validFormats)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = format;
                item.Tag = format;
                this.comboFastqType.Items.Add(item);
            }
            this.comboFastqType.SelectedIndex = 0;
            

            #endregion

            #region BLAST database combo box

            this.availableBlastDatabases = new List<string> ();

            // query for available BLAST databases
            List<string> databases = BlastTools.QueryAvailableBlastDatabases();

            foreach (var db in databases)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = db;
                item.Tag = db;
                this.comboBlastDb.Items.Add(item);
            }

            this.comboBlastDb.SelectedIndex = 0;

            #endregion
        }

        /// <summary>
        /// Opens the OpenFileDialog browse window to select the input file
        /// </summary>
        private void LaunchBrowseInputFileWindow()
        {
            using (System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
            {
                dialog.CheckFileExists = true;
                dialog.CheckPathExists = true;

                // Disable selection of multiple files (will support multiselection in the future)
                dialog.Multiselect = false;

                // Set filters
                dialog.Filter = String.Join("|", fileTypes.ToArray());
                dialog.FilterIndex = 2;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string autoParser = null;
                    bool chooseParserManually = false;

                    #region Validate input

                    // Validate the input file
                    this.input = new InputSubmission(dialog.FileNames[0]);

                    if (this.input.CanParseInputByFileName())
                    {
                        autoParser = this.input.Parser.Name;
                    }
                    else
                    {
                        chooseParserManually = true;
                    }

                    #endregion

                    #region Update fields

                    // Display filename in text box
                    this.textInputFilename.Text = dialog.FileNames[0];

                    // Enable molecule type
                    //this.comboMoleculeType.IsEnabled = true;
                    //this.comboMoleculeType.SelectedIndex = 0;

                    // Enable parser selection
                    this.comboParserType.IsEnabled = true;

                    #endregion

                    #region Update combo box
                    // Enable parser selection if applicable
                    if (chooseParserManually)
                    {
                        this.comboParserType.SelectedIndex = 0;

                        // Inform the user that parser type could not be detected
                        System.Windows.MessageBox.Show(Resource.AUTOPARSE_FAIL);
                    }
                    else
                    {
                        foreach (ComboBoxItem item in this.comboParserType.Items)
                        {
                            string itemParser = item.Content as string;

                            if (itemParser != null)
                            {
                                if (itemParser.Equals(autoParser))
                                {
                                    this.comboParserType.SelectedItem = item;

                                    //// Set QC Analyzer checkboxes
                                    UpdateAnalyzerCheckBoxes(itemParser);

                                    //// Set Blast configuration checkboxes
                                    EnableBlastConfig();

                                    break;
                                }
                            }
                        }

                        if (autoParser.Equals(SequenceParsers.FastQ.Name))
                        {
                            this.comboFastqType.IsEnabled = true;
                            this.comboFastqType.SelectedIndex = 0;
                        }
                        else
                        {
                            this.btnRun.IsEnabled = true;
                            this.btnRun.Focus();
                        }
                    }


                    #endregion

                }
            }
        }

        /// <summary>
        /// Launches the OpenFileDialog, selects the file and validates it
        /// </summary>
        /// <param name="sender">Framework element.</param>
        /// <param name="e">Routed event args.</param>
        private void OnBrowseButtonClick(object sender, RoutedEventArgs e)
        {
            LaunchBrowseInputFileWindow();
        }

        /// <summary>
        /// Handles the selection changed on the Combo box to select Parser type.
        /// This would set the Parser type to be used while parsing the given files.
        /// </summary>
        /// <param name="sender">Framework element</param>
        /// <param name="e">selection changed event args</param>
        private void OnParserSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.comboParserType.SelectedIndex == 0)
            {
                //this.btnImport.IsEnabled = false;
                UpdateAnalyzerCheckBoxes(null);
            }
            else
            {
                ComboBoxItem item = this.comboParserType.SelectedItem as ComboBoxItem;
                UpdateAnalyzerCheckBoxes(item.Content as string);
                EnableBlastConfig();
            }
        }

        /// <summary>
        /// Handles the selection changed on the Combo box to select Fastq Format type.
        /// This would set the Parser type to be used while parsing the given files.
        /// </summary>
        /// <param name="sender">Framework element</param>
        /// <param name="e">selection changed event args</param>
        private void OnFastqTypeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ComboBox).SelectedIndex == 0)
                this.btnRun.IsEnabled = false;
            else
            {
                this.btnRun.IsEnabled = true;
                this.btnRun.Focus();
            }

        }

        /// <summary>
        /// When called, this will update the analyzer check boxes according
        /// to the currently selected parser.
        /// </summary>
        /// <param name="currentParserName">Current parser name</param>
        private void UpdateAnalyzerCheckBoxes(string currentParserName)
        {
            if (currentParserName == null)
            {
                this.sequenceCheckBox.IsEnabled = false;
                this.qualityScoreCheckBox.IsEnabled = false;
                this.qualityScoreCheckBox.IsChecked = false;
                this.blastEnabledCheckBox.IsEnabled = false;
                this.comboFastqType.IsEnabled = false;
                this.btnRun.IsEnabled = false;
            }
            else
            {
                this.sequenceCheckBox.IsChecked = true;
                this.sequenceCheckBox.IsEnabled = true;
                //this.blastExpander.IsEnabled = true;

                // If FastQ, enable quality score analyzer
                if (currentParserName.Equals(SequenceParsers.FastQ.Name))
                {

                    this.qualityScoreCheckBox.IsEnabled = true;
                    this.qualityScoreCheckBox.IsChecked = true;
                    this.comboFastqType.IsEnabled = true;
                    this.comboFastqType.Focus();
                    this.btnRun.IsEnabled = false;
                }
                // If FastA, disable quality score analyzer
                else if (currentParserName.Equals(SequenceParsers.Fasta.Name))
                {
                    this.qualityScoreCheckBox.IsEnabled = false;
                    this.qualityScoreCheckBox.IsChecked = false;
                    this.comboFastqType.IsEnabled = false;
                    this.btnRun.IsEnabled = true;
                    this.btnRun.Focus();
                }
            }
        }

        /// <summary>
        /// When called, this will enable the BLAST configuration fields.
        /// </summary>
        private void EnableBlastConfig()
        {
            this.blastEnabledCheckBox.IsEnabled = true;
        }

        /// <summary>
        /// When BLAST option is enabled via the checkbox, this will 
        /// populate the configuration fields with default settings.
        /// </summary>
        /// <param name="sender">Blast checkbox element.</param>
        /// <param name="e">Selection changed event.</param>
        private void OnBlastEnabledChecked(object sender, RoutedEventArgs e)
        {
            // Set default max sequences to BLAST
            this.textBlastMaxSequences.IsEnabled = true;
            if (this.textBlastMaxSequences.Text.Equals(""))
            {
                this.textBlastMaxSequences.Text = SeqcosApp.Properties.Resource.BLAST_MAX_SEQUENCES_DEFAULT;
            }

            // Set default BLAST database
            this.comboBlastDb.IsEnabled = true;

        }

        /// <summary>
        /// When BLAST Enabled checkbox is unchecked, disable all
        /// config fields
        /// </summary>
        /// <param name="sender">Blast checkbox element.</param>
        /// <param name="e">Selection changed event.</param>
        private void OnBlastEnabledUnchecked(object sender, RoutedEventArgs e)
        {
            this.textBlastMaxSequences.IsEnabled = false;
            this.comboBlastDb.IsEnabled = false;
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
        /// When clicked, proceed to the analysis step.
        /// </summary>
        /// <param name="sender">Framework element.</param>
        /// <param name="e">Routed event args</param>
        private void OnRunClicked(object sender, RoutedEventArgs e)
        {
            // Store BLAST parameters
            this.blastArgs = null;

            if (this.blastEnabledCheckBox.IsChecked.Value)
            {
                this.blastArgs = new UniVecParameters();
                this.blastArgs.Database = ((ComboBoxItem)this.comboBlastDb.SelectedItem).Content as string;
                this.blastArgs.NumInputSequences = Convert.ToInt32(this.textBlastMaxSequences.Text);
            }

            OpenFileArgs runArgs = new OpenFileArgs(this.input, this.sequenceCheckBox.IsChecked.Value, this.qualityScoreCheckBox.IsChecked.Value, blastArgs, ((ComboBoxItem)this.comboFastqType.SelectedItem).Content.ToString());

            // Hide this wondow
            this.Visibility = System.Windows.Visibility.Hidden;
            
            if (this.PrepareRun != null)
            {
                this.PrepareRun(sender, runArgs);
            }
        }

        /// <summary>
        /// Helper method that checks whether all of the required database files
        /// are present.
        /// </summary>
        /// <param name="dbFile">The selected database file from the file browser</param>
        /// <param name="extensions">Extensions to check</param>
        /// <returns>True, if all extensions exist</returns>
        private static bool ValidateExtensions(string dbFile, string[] extensions)
        {
            foreach (var ext in extensions)
            {
                if (!Path.GetExtension(dbFile).Equals(ext))
                {
                    // Check the extension of each file
                    dbFile = Path.ChangeExtension(dbFile, ext);

                    if (!File.Exists(dbFile))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
