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
using System.Drawing;
using ShoNS.Array;
using ShoNS.MathFunc;
using ShoNS.Visualization;

namespace SeqcosApp.Analyzer
{
    /// <summary>
    /// Helper class for creating Sho-related objects
    /// </summary>
    public static class ShoHelper
    {
        // arbitrary width factor in pixels of each element in x axis
        private const int xWidthFactor = 18;

        public const int WidthDefault = 800;
        public const int WidthDefaultMax = 2000;
        public const int HeightDefault = 600;

        /// <summary>
        /// Helper method to create a basic ShoChart object with common parameters pre-defined
        /// </summary>
        /// <param name="hasLegend">Specify if legend should be displayed</param>
        /// <param name="height">Height in pixels</param>
        /// <param name="width">Width in pixels</param>
        /// <param name="yTitle">y-axis title</param>
        /// <param name="xTitle">x-axis title</param>
        /// <param name="title">plot main title</param>
        /// <returns></returns>
        public static ShoChart CreateBasicShoChart(bool hasLegend, int height, int width, string yTitle, string xTitle, string title)
        {
            ShoChart f = new ShoChart();
            f.Hold = true;

            f.HasLegend = hasLegend;

            // Set plot size
            f.Height = height;
            f.Width = width;

            // Set axis and plot titles
            f.XTitle = xTitle;
            f.YTitle = yTitle;
            f.Title = title;

            // Set major and minor gridlines
            //f.VerticalMajorGridlinesVisible = true;

            // Adjust font style and size
            var axisFont = new Font(FontFamily.GenericSansSerif, 10);
            f.XTitleFont = axisFont;
            f.YTitleFont = axisFont;
            f.TitleFont = new Font(FontFamily.GenericSansSerif, 12);

            return f;
        }

        /// <summary>
        /// Determine the width of the plot in pixels. Mainly used for plots
        /// where the x-axis is base position. If the read length is very long,
        /// then we will want to make the width of the plot longer. Otherwise,
        /// use default. Width size is capped at a default maximum.
        /// </summary>
        /// <param name="n">Length of a read</param>
        /// <returns>Width in pixels</returns>
        public static int GetAutoPlotWidth(long n)
        {
            return n > 50 ? 
                    (xWidthFactor * (int)n > WidthDefaultMax ? WidthDefaultMax : xWidthFactor * (int)n) 
                    : WidthDefault;
        }

        /// <summary>
        /// Create a list of integers be used as labels for the ShoChart.
        /// </summary>
        /// <param name="start">Start number</param>
        /// <param name="n">Number of elements</param>
        /// <returns>List of integers from start to start+n-1</returns>
        public static List<int> GetIntegerAxisLabels(int start, int n)
        {
            var elements = new List<int>();

            for (int i = start; i <= n; i++)
            {
                elements.Add(i);
            }

            return elements;
        }

        public static List<int> CreateBins(double min, double max, int optionalInterval = 1)
        {
            int minInt = (int)min;
            int maxInt = (int)max;
            List<int> bins = new List<int>();

            for (int i = minInt; i <= maxInt; i += optionalInterval)
            {
                bins.Add(i);
            }

            return bins;
        }

    }
}