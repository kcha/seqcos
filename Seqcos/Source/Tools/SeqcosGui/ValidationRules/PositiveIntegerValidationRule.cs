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

using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace SeqcosGui.ValidationRules
{
    /// <summary>
    /// Custom validation for checking the read length formatAsString entered the user.
    /// Used by: DiscardByLength
    /// </summary>
    public class PositiveIntegerValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            string newReadLength = value.ToString();

            // check for positive integer
            Regex positiveNumberPattern = new Regex("^[0-9]+$");

            if (!positiveNumberPattern.IsMatch(newReadLength))
                return new ValidationResult(false, SeqcosGui.Properties.Resource.NotPositiveInteger);

            return new ValidationResult(true, null);
        }
    }
}
