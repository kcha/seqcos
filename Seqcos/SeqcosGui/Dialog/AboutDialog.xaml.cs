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
using System.Diagnostics;
using System.Windows;

namespace SeqcosGui.Dialog
{
    /// <summary>
    /// Interaction logic for AboutDialog.xaml
    /// </summary>
    public partial class AboutDialog : Window
    {
        public AboutDialog()
        {
            InitializeComponent();
            this.versionText.Text = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
        }

        /// <summary>
        /// Closes the dialog window
        /// </summary>
        /// <param name="sender">About menu item.</param>
        /// <param name="e">Event data.</param>
        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Handles RequestNavigate event to open a URL
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        /// <summary>
        /// Displays the online user manual by pointing to the URL
        /// </summary>
        /// <param name="sender">About menu item.</param>
        /// <param name="e">Event data.</param>
        /**
        private void OnViewHelpClick(object sender, RoutedEventArgs e)
        {
            Process.Start("http://seqcos.codeplex.com");
        }
         * */

        
    }
}
