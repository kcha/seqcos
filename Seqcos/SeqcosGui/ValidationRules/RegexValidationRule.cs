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

using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace SeqcosGui.ValidationRules
{
    public class RegexValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            string pattern = value.ToString();

            // check that a Regex object can be constructed
            try
            {
                Regex re = new Regex(pattern);
                return new ValidationResult(true, null);
            }
            catch { }

            string msg = string.Format(SeqcosGui.Properties.Resource.NotValidRegex, pattern);
            return new ValidationResult(false, msg);
        }
    }
}
