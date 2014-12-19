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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SeqcosApp;
using SeqcosFilterTools.Discard;
using SeqcosFilterTools.Trim;
using SeqcosGui.Dialog;
using SeqcosGui.Properties;

namespace SeqcosGui
{
    /// <summary>
    /// Interaction logic for SeqcosMainWindow.xaml
    /// </summary>
    public partial class SeqcosMainWindow : Window
    {
        #region Private members

        /// <summary>
        /// List of valid file types.
        /// </summary>
        private List<string> fileTypes;

        /// <summary>
        /// An instance of the QC application framework, where all the actual
        /// QC analysis logic is handled.
        /// </summary>
        private Seqcos application;

        /// <summary>
        /// An instance of the OpenFileDialog for selecting input file
        /// </summary>
        private OpenFileDialog openFile;

        /// <summary>
        /// An instance of the dialog window that is shown while the analysis
        /// is being carried out.
        /// </summary>
        private RunProgressDialog run;

        /// <summary>
        /// Handles running of the QC analysis or post-QC filtering on a separate thread.
        /// </summary>
        private BackgroundWorker analysisThread;

        /// <summary>
        /// Delegate function for updating the progress bar during QC analysis
        /// </summary>
        /// <param name="percentage">Progress in percentage</param>
        /// <param name="text">Status message</param>
        private delegate void UpdateProgressDelegate(int percentage, string text);

        #endregion

        #region Constructor

        /// <summary>
        /// Creates an instance of the main window of the application
        /// </summary>
        public SeqcosMainWindow()
        {
            InitializeComponent();

            this.fileTypes = BioHelper.QuerySupportedFileFilters();
        }

        #endregion

        #region Menu methods

        /// <summary>
        /// Displays About screen
        /// </summary>
        /// <param name="sender">About menu item.</param>
        /// <param name="e">Event data.</param>
        private void OnAboutClicked(object sender, RoutedEventArgs e)
        {
            AboutDialog about = new AboutDialog();
            about.Owner = this;
            about.ShowDialog();
        }

        /// <summary>
        /// Indicates that the user has clicked on Exit option in the file menu.
        /// </summary>
        /// <param name="sender">Exit menu.</param>
        /// <param name="e">Event data.</param>
        private void OnExitMenuClicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        
        /// <summary>
        /// Displays the online user manual by pointing to the URL
        /// </summary>
        /// <param name="sender">Help menu item.</param>
        /// <param name="e">Event data.</param>
        private void OnViewHelpClicked(object sender, RoutedEventArgs e)
        {
            Process.Start("http://seqcos.codeplex.com/documentation");
            //MessageBox.Show("Under construction");
        }

        #endregion

        #region Running analysis methods

        /// <summary>
        /// Displays the OpenFileDialog screen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNewRunClicked(object sender, RoutedEventArgs e)
        {
            openFile = new OpenFileDialog(this.fileTypes);
            openFile.PrepareRun += new EventHandler<OpenFileArgs>(this.OnRunStartClicked);

            openFile.Owner = this;
            
            bool? continueRun = openFile.ShowDialog();
        }

        /// <summary>
        /// This event is fired when OpenFileDialog raises this event
        /// indicating that the user has selected to run the QC analysis
        /// </summary>
        /// <param name="sender">OpenFileDialog</param>
        /// <param name="e">OpenFileDialog arguments</param>
        private void OnRunStartClicked(object sender, OpenFileArgs e)
        {
            run = new RunProgressDialog();
            run.StartRun += new EventHandler(this.OnRunQcAnalysisStarted);
            run.CancelRun += new EventHandler(this.OnCancelAnalysisClicked);

            run.Args = e;
            run.Owner = this;
            this.IsEnabled = false;
            run.ShowDialog();
            this.IsEnabled = true;
        }

        /// <summary>
        /// This event is fired when the RunProgressDialog is opened and initiates
        /// the QC analysis of the given input file.
        /// </summary>
        /// <param name="sender">RunProgressDialog element</param>
        /// <param name="e">Event args</param>
        private void OnRunQcAnalysisStarted(object sender, EventArgs e)
        {
            ClearPreviousRun();
            this.application = null;

            this.analysisThread = new BackgroundWorker();
            this.analysisThread.WorkerSupportsCancellation = true;
            this.analysisThread.WorkerReportsProgress = true;
            this.analysisThread.DoWork += new DoWorkEventHandler(this.DoQcAnalysis);
            this.analysisThread.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
            {
                this.run.Close();

                if (!args.Cancelled)
                {
                    // Load results
                    LoadRunResults(application.OutputDirectory, this.application.FileList);
                }
            };
            this.analysisThread.RunWorkerAsync(e as OpenFileArgs);
        }


        /// <summary>
        /// This event is fired by analysisThread when the thread is invoked.
        /// This event calls the main Qc application framework and executes the analysis.
        /// </summary>
        /// <param name="sender">BackgroundWorker instance</param>
        /// <param name="e">Event parameters</param>
        private void DoQcAnalysis(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            
            if (worker != null)
            {
                try
                {
                    OpenFileArgs args = e.Argument as OpenFileArgs;
                    System.Windows.Threading.Dispatcher dispatcher = run.Dispatcher;
                    UpdateProgressDelegate update = new UpdateProgressDelegate(UpdateProgressText);

                    if (args != null)
                    {
                        dispatcher.BeginInvoke(update, 0, "Starting analysis...please be patient!");
                        application = new Seqcos(args.InputInfo.Parser, args.InputInfo.Filename, args.CanRunSequenceQc, args.CanRunQualityScoreQc, args.CanRunBlast, args.FastqFormat);
                        
                        #region Sequence-level QC
                
                        /// Run sequence level QC
                        if (args.CanRunSequenceQc)
                        {
                            // Content by position
                            dispatcher.BeginInvoke(update, 25, "Performing sequence-level QC...analyzing base positions");
                            application.SequenceQc.ContentByPosition();
                            dispatcher.BeginInvoke(update, 45, "Performing sequence-level QC...analyzing base positions");
                            application.SequenceQc.GCContentByPosition();
                            System.Threading.Thread.Sleep(100);
                            if (worker.CancellationPending)
                            {
                                e.Cancel = true;
                                return;
                            }

                            // Content by sequence
                            dispatcher.BeginInvoke(update, 65, "Performing sequence-level QC...analyzing sequences");
                            application.SequenceQc.ContentBySequence();
                            dispatcher.BeginInvoke(update, 85, "Performing sequence-level QC...analyzing sequences");
                            System.Threading.Thread.Sleep(100);
                            if (worker.CancellationPending)
                            {
                                e.Cancel = true;
                                return;
                            }

                            // Plot results
                            dispatcher.BeginInvoke(update, 80, "Generating plots...");
                            application.PlotSequenceLevelStats();
                            System.Threading.Thread.Sleep(100);
                            if (worker.CancellationPending)
                            {
                                e.Cancel = true;
                                return;
                            }

                            dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Send, update, 100, "Done sequence-level QC!");
                        }
                        #endregion
                        
                        #region Quality score-level QC

                        /// Run quality score level QC
                        if (args.CanRunQualityScoreQc)
                        {
                            dispatcher.BeginInvoke(update, 5, "Performing quality score-level QC...analyzing base positions");
                            application.QualityScoreQc.ContentByPosition();
                            System.Threading.Thread.Sleep(100);
                            if (worker.CancellationPending)
                            {
                                e.Cancel = true;
                                return;
                            }

                            dispatcher.BeginInvoke(update, 25, "Performing quality score-level QC...analyzing sequences");
                            application.QualityScoreQc.ContentBySequence();
                            System.Threading.Thread.Sleep(100);
                            if (worker.CancellationPending)
                            {
                                e.Cancel = true;
                                return;
                            }

                            dispatcher.BeginInvoke(update, 55, "Generating plots (the boxplot will take a while...please be patient!)");
                            application.PlotQualityScoreLevelStats();
                            System.Threading.Thread.Sleep(100);
                            if (worker.CancellationPending)
                            {
                                e.Cancel = true;
                                return;
                            }

                            dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Send, update, 100, "Done quality score-level QC!");
                        }

                        application.WriteInputStatistics(excelFormat: false);
                        application.FinishQualityScoreQC();

                        #endregion

                        #region BLAST

                        /// Run BLAST
                        if (args.CanRunBlast)
                        {
                            dispatcher.BeginInvoke(update, 10, "Generating temporary FASTA file for BLAST...");

                            // execute BLAST here..
                            string targetFasta = application.OutputDirectory + "/" + application.GetPrefix() + ".fa";
                            BioHelper.ConvertToFASTA(application.ContaminationFinder.TargetSequences, targetFasta, args.BlastArgs.NumInputSequences, false);

                            dispatcher.BeginInvoke(update, 70, "Running BLAST...");
                            application.ContaminationFinder.RunLocalBlast(args.BlastArgs.Database, targetFasta);

                            dispatcher.BeginInvoke(update, 100, "Finished! Deleting FASTA file...");
                            File.Delete(targetFasta);
                        }

                        #endregion
                    }
                }
                catch (ArgumentNullException ex)
                {
                    e.Cancel = true;
                    if (worker.CancellationPending)
                    {
                        return;
                    }

                    MessageBox.Show(ex.TargetSite + ": " + ex.Message);
                }
                catch (Exception ex)
                {
                    e.Cancel = true;
                    if (worker.CancellationPending)
                    {
                        return;
                    }

                    MessageBox.Show(ex.TargetSite + ": " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Delegate function for updating the progress bar and text message during QC analysis.
        /// </summary>
        /// <param name="percentage">Progress based on percentage</param>
        /// <param name="text">Update text message</param>
        private void UpdateProgressText(int percentage, string text)
        {
            this.run.ProgressText = text;
            this.run.ProgressValue = percentage;
        }

        /// <summary>
        /// This event is raised when the user clicks on Cancel during the QC
        /// analysis. This causes the BackgroundWorker thread to be cancelled
        /// and halt operation.
        /// </summary>
        /// <param name="sender">Background worker</param>
        /// <param name="e">Event args</param>
        private void OnCancelAnalysisClicked(object sender, EventArgs e)
        {
            if (this.analysisThread.IsBusy)
            {
                this.analysisThread.CancelAsync();
            }
        }

        #endregion

        #region Loading results methods

        /// <summary>
        /// This event is raised when the user loads an existing directory
        /// containing image files generated from a previous run.
        /// </summary>
        /// <param name="sender">Main window element</param>
        /// <param name="e">Routed event args</param>
        private void OnLoadRunClicked(object sender, RoutedEventArgs e)
        {
            // Need to find something better than FolderBrowserDialog...hard to use and doesn't remember previous location.
            using (System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.ShowNewFolderButton = false;
                dialog.Description = "Select a folder containing a previous QC run:";

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string prefix = Path.GetFileNameWithoutExtension(dialog.SelectedPath);

                    LoadRunResults(dialog.SelectedPath, null);
                }
            }
        }

        /// <summary>
        /// This will load the results from the completed QC analysis and
        /// display on the SeqcosMainWindow.
        /// </summary>
        /// <param name="dir">Directory name of files</param>
        /// <returns>A collection of Image control objects</returns>
        private void LoadRunResults(string dir, Filenames fileList)
        {
            if (!Directory.Exists(dir))
            {
                throw new DirectoryNotFoundException();
            }

            if (fileList == null)
            {
                fileList = new Filenames(dir, Resource.ChartFormat);
            }

            ClearPreviousRun();

            // Update TabControl panels
            UpdateTabControl(dir, fileList.GetSequenceLevelFilenames(), this.sequenceTab);
            UpdateTabControl(dir, fileList.GetQualityLevelFilenames(), this.qualityTab);

            // Issue a warning if the selected directory doesn't contain any relevant files
            if (this.sequenceTab.Children.Count == 0 && this.qualityTab.Children.Count == 0)
            {
                MessageBox.Show(Resource.LoadRunFail, Resource.LoadRunFailCaption, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else
            {
                // Update input statistics
                UpdateInputStatistics(dir + @"\" + fileList.CSV);

                if (application != null)
                    OpenInExplorer(application.OutputDirectory);
            }

        }

        /// <summary>
        /// Updates the TabControl panel with image thumbnails
        /// </summary>
        /// <param name="dir">Directory name of files</param>
        /// <param name="plotFileList">List of filenames to load</param>
        /// <param name="panel">The control panel that will be updated with Image controls</param>
        private void UpdateTabControl(string dir, List<string> plotFileList, Panel panel)
        {
            if (panel != null)
            {
                // Examine the files in dir and look for the plot-related files
                foreach (string file in Directory.GetFiles(dir))
                {
                    if (plotFileList.Contains(Path.GetFileName(file)))
                    {
                        Border border = new Border();
                        border.Margin = new Thickness(4, 0, 4, 0);
                        border.BorderThickness = new Thickness(2);
                        border.MouseEnter += delegate(object s, System.Windows.Input.MouseEventArgs e)
                        {
                            // Color the border when the mouse hovers over the thumbnail
                            (s as Border).BorderBrush = Brushes.Red; 
                        };
                        border.MouseLeave += delegate(object s, System.Windows.Input.MouseEventArgs e)
                        {
                            (s as Border).BorderBrush = null;
                        };

                        // Get the image control for thumbnail
                        Image img = CreateAThumbnail(file);
                        // Attach to Border parent element
                        border.Child = img;
                        // Attach to tab control
                        panel.Children.Add(border);
                    }
                }
            }
        }

        /// <summary>
        /// Create a thumbnail of an image
        /// </summary>
        /// <param name="file">Image filename</param>
        /// <returns>An Image control object</returns>
        private Image CreateAThumbnail(string file)
        {
            if (!File.Exists(file))
                throw new FileNotFoundException();
        
            // Create a new Image control object
            Image thumbnailImage = new Image();
            thumbnailImage.Width = 80;
            thumbnailImage.MouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(OnThumbnailMouseLeftDown);
            thumbnailImage.ToolTip = Path.GetFileName(file);
            

            // Add context menu to Image control
            thumbnailImage.ContextMenu = CreateThumbnailContextMenu(Path.GetFileName(file));

            // Create a bitmapimage and set its DecodePixelWidth and DecodePixel Height
            BitmapImage bmpImage = new BitmapImage();
            bmpImage.BeginInit();
            bmpImage.UriSource = new Uri(file);
            bmpImage.DecodePixelWidth = 80;
            bmpImage.DecodePixelHeight = 80;
            bmpImage.EndInit();
            // Set Source property of Image  
            thumbnailImage.Source = bmpImage;

            return thumbnailImage;
        }

        /// <summary>
        /// Create a ContextMenu for the Thumbnail
        /// </summary>
        /// <param name="name">The filename of the thumbnail</param>
        /// <returns>A ContextMenu object</returns>
        private ContextMenu CreateThumbnailContextMenu(string name)
        {
            ContextMenu mainMenu = new ContextMenu();

            MenuItem filenameItem = new MenuItem();
            filenameItem.Header = name;
            filenameItem.IsEnabled = false;
            mainMenu.Items.Add(filenameItem);

            MenuItem viewItem = new MenuItem();
            viewItem.Header = FindResource("Thumbnail_ContextMenu_View");
            viewItem.Click += new RoutedEventHandler(OnThumbnailViewClick);
            mainMenu.Items.Add(viewItem);

            MenuItem openInExplorerItem = new MenuItem();
            openInExplorerItem.Header = FindResource("Thumbnail_ContextMenu_OpenInExplorer");
            openInExplorerItem.Click += new RoutedEventHandler(OnThumbnailExplorerClick);
            mainMenu.Items.Add(openInExplorerItem);

            return mainMenu;
        }

        /// <summary>
        /// This event is raised when the user selects to view a thumbnail 
        /// from the ContextMenu.
        /// </summary>
        /// <param name="sender">MenuItem</param>
        /// <param name="e">Routed event args</param>
        private void OnThumbnailViewClick(object sender, RoutedEventArgs e)
        {
            MenuItem menu = sender as MenuItem;
            ContextMenu cmenu = menu.Parent as ContextMenu;
            OnThumbnailMouseLeftDown(cmenu.PlacementTarget, e);
        }

        /// <summary>
        /// This event is raised when the user selects from the ContextMenu
        /// to open the location of the thumbnail via Window Explorer
        /// </summary>
        /// <param name="sender">MenuItem</param>
        /// <param name="e">Routed event args</param>
        private void OnThumbnailExplorerClick(object sender, RoutedEventArgs e)
        {
            MenuItem menu = sender as MenuItem;
            ContextMenu cmenu = menu.Parent as ContextMenu;
            Image img = cmenu.PlacementTarget as Image;
            BitmapImage bimg = img.Source as BitmapImage;

            OpenInExplorer(bimg.UriSource.AbsoluteUri);
        }

        /// <summary>
        /// This event is fired when the user selects from the ContextMenu to open
        /// the location of the main image via Windows Explorer
        /// </summary>
        /// <param name="sender">MenuItem</param>
        /// <param name="e">Routed event args</param>
        private void OnChartAreaExplorerClick(object sender, RoutedEventArgs e)
        {
            MenuItem menu = sender as MenuItem;
            ContextMenu cmenu = menu.Parent as ContextMenu;
            Image img = cmenu.PlacementTarget as Image;
            BitmapImage bimg = img.Source as BitmapImage;

            OpenInExplorer(bimg.UriSource.AbsoluteUri);
        }

        /// <summary>
        /// Opens an instance of Windows Explorer and selects the specified 
        /// file.
        /// </summary>
        /// <param name="selectedFile">The file to be selected in Explorer</param>
        private void OpenInExplorer(string selectedFile)
        {
            Process.Start(Resource.Explorer, "/select, " + selectedFile);
        }

        /// <summary>
        /// This event is raised when the user left clicks on a thumbnail. It
        /// will trigger the main chart area to update its display with the image
        /// of the thumbnail.
        /// </summary>
        /// <param name="sender">Image control element</param>
        /// <param name="e">Routed event args</param>
        private void OnThumbnailMouseLeftDown(object sender, RoutedEventArgs e)
        {
            Image img = sender as Image;
            BitmapImage imgSource = img.Source as BitmapImage;

            // Check if the user clicked on an image that is currently already on display
            string currentUri = null;
            if (this.imageMainChart.Source != null)
            {
                currentUri = ((BitmapImage)this.imageMainChart.Source).UriSource.AbsoluteUri;

                if (currentUri.Equals(imgSource.UriSource.AbsoluteUri))
                {
                    return;
                }
            }

            // Create a new BitmapImage source
            BitmapImage newBmpImage = new BitmapImage();
            newBmpImage.BeginInit();
            newBmpImage.UriSource = imgSource.UriSource;
            newBmpImage.EndInit();

            // Set the new source
            this.imageMainChart.Source = newBmpImage;
            this.menuMainChartOpen.IsEnabled = true;

            // Update the image properties
            UpdateImageProperties(newBmpImage);
        }

        /// <summary>
        /// When the image source is changed in the chart area, this method
        /// is called to update the image properties text box.
        /// </summary>
        /// <param name="bmImage">BitmapImage object holding the image source</param>
        private void UpdateImageProperties(BitmapImage bmImage)
        {
            try
            {
                string imagePath = bmImage.UriSource.AbsolutePath;
                imagePath = System.Text.RegularExpressions.Regex.Replace(imagePath, "%20", " ");

                StringBuilder text = new StringBuilder();
                FileInfo info = new FileInfo(imagePath);

                text.Append(string.Format("Filename: {0}\n\n", info.Name));
                text.Append(string.Format("Directory: {0}\n\n", info.DirectoryName));
                text.Append(string.Format("Size: {0} KB\n\n", Math.Round(info.Length / 1024f, 1)));
                text.Append(string.Format("Updated: {0}\n\n", info.LastWriteTime));

                this.textImageProperties.Text = text.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Update image properties error: " + ex.Message);
            }
        }

        /// <summary>
        /// When a new analysis is loaded, this method will update the input summary statistics panel.
        /// </summary>
        /// <param name="csvFile">Filename of the CSV file</param>
        private void UpdateInputStatistics(string csvFile)
        {
            // Need to be able to either read from CSV file or read from application
            InputStatistics stats = new InputStatistics(csvFile);

            // Update total number of reads
            this.totalReadsValue.Text = " " + stats.NumberOfReads.ToString();

            this.readLengthMinValue.Text = String.Format("{0:F1}", stats.ReadLengthMin);
            this.readLengthValue.Text = String.Format("{0:F1}", stats.ReadLengthMax);
            this.readLengthMeanValue.Text = String.Format("{0:F1}", stats.ReadLengthMean);

            this.readGcContentMinValue.Text = String.Format("{0:F1}", stats.ReadGcContentMin);
            this.readGcContentMaxValue.Text = String.Format("{0:F1}", stats.ReadGcContentMax);
            this.readGcContentMeanValue.Text = String.Format("{0:F1}", stats.ReadGcContentMean);

            this.basePhredMinValue.Text = String.Format("{0:F1}", stats.BasePhredScoreMin);
            this.basePhredMaxValue.Text = String.Format("{0:F1}", stats.BasePhredScoreMax);
            this.basePhredMeanValue.Text = String.Format("{0:F1}", stats.BasePhredScoreMean);

            this.readPhredMinValue.Text = String.Format("{0:F1}", stats.ReadPhredScoreMin);
            this.readPhredMaxValue.Text = String.Format("{0:F1}", stats.ReadPhredScoreMax);
            this.readPhredMeanValue.Text = String.Format("{0:F1}", stats.ReadPhredScoreMean);
        }

        /// <summary>
        /// Clear results from previous loaded runs
        /// </summary>
        private void ClearPreviousRun()
        {
            this.imageMainChart.Source = null;
            this.sequenceTab.Children.Clear();
            this.qualityTab.Children.Clear();
            this.textImageProperties.Text = null;

            this.totalReadsValue.Text = null;

            this.readLengthMinValue.Text = null;
            this.readLengthValue.Text = null;
            this.readLengthMeanValue.Text = null;

            this.readGcContentMinValue.Text = null;
            this.readGcContentMaxValue.Text = null;
            this.readGcContentMeanValue.Text = null;

            this.basePhredMinValue.Text = null;
            this.basePhredMaxValue.Text = null;
            this.basePhredMeanValue.Text = null;

            this.readPhredMinValue.Text = null;
            this.readPhredMaxValue.Text = null;
            this.readPhredMeanValue.Text = null;
        }

        #endregion

        #region Trim/Discard

        /// <summary>
        /// This event is raised when the user clicks on the Trim By Length or Trim By Quality Score
        /// menu item. It will start the Trim dialog.
        /// </summary>
        /// <param name="sender">MenuItem element</param>
        /// <param name="e">Routed event args</param>
        private void OnTrimClick(object sender, RoutedEventArgs e)
        {
            MenuItem menu = sender as MenuItem;
            TrimDialog trim = null;

            /// If an instance of SeqcosMainWindow is available, populate the input file
            /// with the input filename. Otherwise, leave blank.
            if (this.application != null)
            {
                trim = new TrimDialog(this.fileTypes, this.application.FileList.FileName, this.application.SelectedParser);
            }
            else
            {
                trim = new TrimDialog(this.fileTypes);
            }

            trim.PrepareToTrim += new EventHandler<FilterToolArgs>(this.OnTrimRunClicked);
            trim.Owner = this;

            // Determine which tab should be in focus upon opening
            if (menu.Uid.Equals("TrimByLength"))
            {
                trim.tabByLength.IsSelected = true;
                trim.trimLengthValue.Focus();
            }
            else if (menu.Uid.Equals("TrimByQualityScore"))
            {
                trim.tabByQuality.IsSelected = true;
                trim.trimQualityValue.Focus();
            }
            else if (menu.Uid.Equals("TrimByRegex"))
            {
                trim.tabByRegex.IsSelected = true;
                trim.trimRegexPattern.Focus();
            }
            trim.ShowDialog();
        }

        /// <summary>
        /// This event is raised when the user clicks on the Discard By Length or Discard By Quality Score
        /// menu item. It will start the Discard dialog.
        /// </summary>
        /// <param name="sender">MenuItem element</param>
        /// <param name="e">Routed event args</param>
        private void OnDiscardClick(object sender, RoutedEventArgs e)
        {
            MenuItem menu = sender as MenuItem;
            DiscardDialog discard = null;

            // If an instance of SeqcosMainWindow is avaliable, populate the input file
            // with the input filename. Otherwise, leave blank.
            if (this.application != null)
            {
                discard = new DiscardDialog(this.fileTypes, this.application.FileList.FileName, this.application.SelectedParser);
            }
            else
            {
                discard = new DiscardDialog(this.fileTypes);
            }

            discard.PrepareToDiscard += new EventHandler<FilterToolArgs>(this.OnDiscardRunClick);
            discard.Owner = this;

            if (menu.Uid.Equals("DiscardByLength"))
            {
                discard.tabByLength.IsSelected = true;
                discard.discardLengthValue.Focus();
            }
            else if (menu.Uid.Equals("DiscardByQualityScore"))
            {
                discard.tabByQuality.IsSelected = true;
                discard.discardQualityValue.Focus();
            }
            else if (menu.Uid.Equals("DiscardByRegex"))
            {
                discard.tabByRegex.IsSelected = true;
                discard.discardRegexPattern.Focus();
            }
            discard.ShowDialog();
        }

        /// <summary>
        /// This event is raised when the user selects to run the trim tool
        /// after specifying all the required input parameters.
        /// </summary>
        /// <param name="sender">TrimToolDialog window</param>
        /// <param name="e">Trim arguments</param>
        private void OnTrimRunClicked(object sender, FilterToolArgs e)
        {
            run = new RunProgressDialog(true);

            run.ProgressText = "Trimming reads...";
            run.StartRun += new EventHandler(this.OnRunTrimStarted);
            run.CancelRun += new EventHandler(this.OnCancelAnalysisClicked);

            run.Args = e;
            run.Owner = this;
            this.IsEnabled = false;
            run.ShowDialog();
            this.IsEnabled = true;
        }

        /// <summary>
        /// This event is raised when the user selects to run the discard tool
        /// after specifying all the required input parameters.
        /// </summary>
        /// <param name="sender">DiscardToolDialog window</param>
        /// <param name="e">Discard arguments</param>
        private void OnDiscardRunClick(object sender, FilterToolArgs e)
        {
            run = new RunProgressDialog(true);

            run.ProgressText = "Discarding reads...";
            run.StartRun += new EventHandler(this.OnRunDiscardStarted);
            run.CancelRun += new EventHandler(this.OnCancelAnalysisClicked);

            run.Args = e;
            run.Owner = this;
            this.IsEnabled = false;
            run.ShowDialog();
            this.IsEnabled = true;
        }

        /// <summary>
        /// This event is raised by the RunProgressDialog to start the trim analysis
        /// </summary>
        /// <param name="sender">RunProgressDialog element</param>
        /// <param name="e">Event args</param>
        private void OnRunTrimStarted(object sender, EventArgs e)
        {
            InitializeBackgroundWorkerThread();
            if (this.analysisThread != null)
            {
                this.analysisThread.DoWork += new DoWorkEventHandler(this.DoTrimReads);
                this.analysisThread.RunWorkerAsync(e as FilterToolArgs);
            }
        }

        /// <summary>
        /// This event is raised by the RunProgressDialog to start the discard analysis
        /// </summary>
        /// <param name="sender">RunProgressDialog element</param>
        /// <param name="e">Event args</param>
        private void OnRunDiscardStarted(object sender, EventArgs e)
        {
            InitializeBackgroundWorkerThread();
            if (this.analysisThread != null)
            {
                this.analysisThread.DoWork += new DoWorkEventHandler(this.DoDiscardReads);
                this.analysisThread.RunWorkerAsync(e as FilterToolArgs);
            }
        }

        /// <summary>
        /// Helper method to initialize a new BackgroundWorker thread
        /// </summary>
        private void InitializeBackgroundWorkerThread()
        {
            this.analysisThread = new BackgroundWorker();
            this.analysisThread.WorkerSupportsCancellation = true;
            this.analysisThread.WorkerReportsProgress = true;
            this.analysisThread.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.OnRunFilterToolCompleted);
            this.analysisThread.ProgressChanged += new ProgressChangedEventHandler(this.OnRunFilterToolProgressChanged);
        }

        /// <summary>
        /// This event is raised when the worker thread updates the progress value
        /// </summary>
        /// <param name="sender">RunProgressDialog element</param>
        /// <param name="e">Progress changed event args</param>
        private void OnRunFilterToolProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            RunProgressDialog progress = sender as RunProgressDialog;
            progress.ProgressValue = e.ProgressPercentage;
        }

        /// <summary>
        /// This executes the trimming of reads as a background thread.
        /// </summary>
        /// <param name="sender">BackgoundWorker instance</param>
        /// <param name="e">DoWorkEvent arguments</param>
        private void DoTrimReads(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            if (worker != null)
            {
                try
                {
                    Trimmer myTrimmer = null;
                    FilterToolArgs args = null;

                    if (e.Argument is TrimByLengthArgs)
                    {
                        args = e.Argument as TrimByLengthArgs;
                        myTrimmer = (e.Argument as TrimByLengthArgs).trimmer;
                        
                        // If possible, verify that the trim length is not larger than the maximum read length, if it
                        // is known (e.g. if a QC analysis was run just before Trim was called).
                        if ((myTrimmer as TrimByLength).TrimLength >= 1)
                        {
                            MessageBoxResult tooLargeReadLength = TryConfirmLargeReadLength(args.InputInfo.Filename, 
                                                            Convert.ToInt64(Math.Round((myTrimmer as TrimByLength).TrimLength)));
                            if (tooLargeReadLength == MessageBoxResult.Cancel)
                            {
                                e.Cancel = true;
                                return;
                            }
                        }
                    }
                    else if (e.Argument is TrimByQualityArgs)
                    {
                        args = e.Argument as TrimByQualityArgs;
                        myTrimmer = (e.Argument as TrimByQualityArgs).trimmer;
                    }
                    else
                    {
                        args = e.Argument as TrimByRegexArgs;
                        myTrimmer = (e.Argument as TrimByRegexArgs).trimmer;
                    }
                    myTrimmer.Worker = worker;
                    myTrimmer.WorkerArgs = e;
                    myTrimmer.TrimAll();

                    // Report results
                    StringBuilder report = new StringBuilder();
                    report.AppendLine(string.Format(Resource.TrimReport, myTrimmer.TrimCount, myTrimmer.Counted));
                    report.AppendLine(string.Format(Resource.DiscardReport, myTrimmer.DiscardCount, myTrimmer.Counted));

                    this.OpenInExplorer(args.OutputFilename);

                    e.Result = report.ToString();
                }
                catch (ArgumentNullException ex)
                {
                    e.Cancel = true;
                    if (worker.CancellationPending)
                    {
                        return;
                    }
                    MessageBox.Show(ex.TargetSite + ": " + ex.Message);
                }
                catch (Exception ex)
                {
                    e.Cancel = true;
                    if (worker.CancellationPending)
                    {
                        return;
                    }
                    MessageBox.Show(ex.TargetSite + ": " + ex.Message);
                }
            }
        }

        /// <summary>
        /// This executes the discarding of reads on a background thread
        /// </summary>
        /// <param name="sender">BackgoundWorker instance</param>
        /// <param name="e">DoWorkEvent arguments</param>
        private void DoDiscardReads(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            if (worker != null)
            {
                try
                {
                    Discarder myDiscarder = null;
                    FilterToolArgs args = null;

                    if (e.Argument is DiscardByLengthArgs)
                    {
                        args = e.Argument as DiscardByLengthArgs;
                        myDiscarder = ((DiscardByLengthArgs)args).discarder;

                        // If possible, verify that the discard length is not larger than the maximum read length.
                        MessageBoxResult tooLargeReadLength = TryConfirmLargeReadLength(args.InputInfo.Filename, (myDiscarder as DiscardByLength).MinLengthThreshold);
                        if (tooLargeReadLength == MessageBoxResult.Cancel)
                        {
                            e.Cancel = true;
                            return;
                        }
                    }
                    else if (e.Argument is DiscardByMeanQualityArgs)
                    {
                        args = e.Argument as DiscardByMeanQualityArgs;
                        myDiscarder = (e.Argument as DiscardByMeanQualityArgs).discarder;
                    }
                    else
                    {
                        args = e.Argument as DiscardByRegexArgs;
                        myDiscarder = (e.Argument as DiscardByRegexArgs).discarder;
                    }
                    myDiscarder.DiscardReads();

                    // Report results
                    string report = string.Format(Resource.DiscardReport, myDiscarder.DiscardCount, myDiscarder.Counted);

                    this.OpenInExplorer(args.OutputFilename);

                    e.Result = report.ToString();
                }
                catch (Exception ex)
                {
                    e.Cancel = true;
                    if (worker.CancellationPending)
                    {
                        return;
                    }
                    MessageBox.Show(ex.TargetSite + ": " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Determine if the input read length may be larger
        /// than the maximum read length in the file. 
        /// This is only known if the user had run a QC step on the same file
        /// immediately prior to this.
        /// </summary>
        /// <param name="filename">The input filename</param>
        /// <param name="length">The user-inputted read length</param>
        /// <returns>A MessageBoxResult</returns>
        private MessageBoxResult TryConfirmLargeReadLength(string filename, long length)
        {
            if (this.application != null &&
                filename.Equals(this.application.FileList.FileName) &&
                length > this.application.ReadLengthMax)
            {
                string msg = string.Format(Resource.ReadLengthTooLarge, this.application.ReadLengthMax, this.application.FileList.FileName);
                MessageBoxResult result = MessageBox.Show(msg, Resource.ReadLengthTooLargeCaption, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                return result;
            }

            return MessageBoxResult.None;
        }

        /// <summary>
        /// This event is raised when the Trim/Discard operation is completed.
        /// </summary>
        /// <param name="sender">BackgroundWorker instance</param>
        /// <param name="e">Run worker completed event args</param>
        private void OnRunFilterToolCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                this.run.Close();
            }
            else
            {
                /// Replace the progress bar with a Close button
                this.run.progressGrid.Children.Clear();

                // Create a new button
                Button btn = new Button();
                btn.Content = "Close";
                btn.Click += delegate(object s, RoutedEventArgs ee)
                {
                    this.run.Close();
                };
                btn.Height = this.run.btnCancel.Height;
                btn.Width = this.run.btnCancel.Width;

                // Add the button to the progress bar
                this.run.progressGrid.Children.Add(btn);

                // Disable Cancel button
                this.run.btnCancel.IsEnabled = false;

                // Update the progress 
                this.run.ProgressText = e.Result as string;
            }
        }

        /// <summary>
        /// Performs zooming effects on the main image when the wheel mouse gets scrolled.
        /// </summary>
        /// <param name="sender">Image control element</param>
        /// <param name="e">Mouse wheel event args</param>
        private void OnMainImageMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            double height = imageMainChart.ActualHeight;
            double width = imageMainChart.ActualWidth;

            height += e.Delta;
            width += e.Delta;

            imageMainChart.Height = height < 150 ? 150 : height;
            imageMainChart.Width = width < 200 ? 200 : width;
        }

        /// <summary>
        /// When the window is requested to be closed, this event displays a 
        /// prompt to confirm the closing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            var result = MessageBox.Show(Resource.CloseConfirm, "Confirm Exit", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.No) { e.Cancel = true; }
        }

        #endregion
    }
}
