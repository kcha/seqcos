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
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SeqcosGui.Properties;

namespace SeqcosGui.Dialog
{
    /// <summary>
    /// Interaction logic for RunProgressDialog.xaml
    /// </summary>
    public partial class RunProgressDialog : Window
    {
        #region Private members

        private delegate void ConfirmCancelDelegate(object sender, RoutedEventArgs e);

        #endregion

        #region Public members

        /// <summary>
        /// Holds the arguments necessary for executing the QC analysis
        /// </summary>
        public EventArgs Args;

        /// <summary>
        /// Sets the progress text 
        /// </summary>
        public string ProgressText
        {
            set
            {
                this.statusTextBox.Text = value;
            }
        }

        /// <summary>
        /// Sets the progress bar value
        /// </summary>
        public int ProgressValue
        {
            set
            {
                this.progressBar.Value = value;
            }
        }


        #endregion

        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of the RunProgressDialog class.
        /// </summary>
        public RunProgressDialog(bool useCircleProgress = false)
        {
            InitializeComponent();

            if (useCircleProgress)
            {
                this.progressBar.Visibility = Visibility.Collapsed;
                CircleProgressAnimation circle = new CircleProgressAnimation();
                this.progressGrid.Children.Add(circle);
            }
        }

        #endregion

        #region Public events

        /// <summary>
        /// Event to indicate that the analysis (QC or post-QC filtering)
        /// should start running.
        /// </summary>
        public EventHandler StartRun;

        /// <summary>
        /// Event to indicate that the analysis should stop its operation.
        /// </summary>
        public EventHandler CancelRun;

        #endregion

        #region Private methods

        /// <summary>
        /// When called, the CancelRun event handler will be fired to 
        /// stop the background thread from running. This dialog window
        /// will also be closed.
        /// </summary>
        /// <param name="sender">Framework element</param>
        /// <param name="e">Routed event args</param>
        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            if (this.CancelRun != null)
            {
                System.Windows.Threading.Dispatcher dispatcher = this.Dispatcher;
                ConfirmCancelDelegate cancel = new ConfirmCancelDelegate(ConfirmCancel);
                dispatcher.Invoke(cancel, sender, e);
            }
        }

        /// <summary>
        /// When a cancel event is raised by the user, this method prompts the user to confirm
        /// whether to continue with the cancel or to continue with the analysis.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfirmCancel(object sender, RoutedEventArgs e)
        {
            // Prompt user to confirm cancel
            var result = MessageBox.Show(Resource.CancelConfirm, "Confirm Cancel", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                this.btnCancel.Content = "Cancelling...";
                this.btnCancel.IsEnabled = false;
                this.ProgressText = "Please wait while we cancel the analysis.";
                this.CancelRun(sender, e);
            }
        }

        /// <summary>
        /// When this dialog is activated, the QC analysis is
        /// immediately launched.
        /// </summary>
        /// <param name="sender">Window element</param>
        /// <param name="e">Routed event args</param>
        private void OnWindowContentRendered(object sender, EventArgs e)
        {
            try
            {
                BeginAnalysis(sender);
            }
            catch (ArgumentNullException ex)
            {
                MessageBox.Show(ex.Message, null, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        
        /// <summary>
        /// When called, this will fire the event to start QC analysis.
        /// </summary>
        /// <param name="sender">Window element</param>
        /// <param name="e">Routed event args</param>
        public void BeginAnalysis(object sender)
        {
            // FileArgs should be set from the parent window
            if (Args == null)
            {
                throw new ArgumentNullException("Event Args has not been set");
            }

            if (this.StartRun != null)
            {
                this.StartRun(sender, Args);
            }
        }

        #endregion
    }
}
