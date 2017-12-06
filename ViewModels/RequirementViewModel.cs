﻿using Jamiras.DataModels;
using Jamiras.ViewModels;
using RATools.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace RATools.ViewModels
{
    public class RequirementViewModel : ViewModelBase
    {
        public RequirementViewModel(Requirement requirement, NumberFormat numberFormat, IDictionary<int, string> notes)
        {
            Requirement = requirement;

            if (requirement != null)
            {
                UpdateDefinition(numberFormat);

                if (requirement.Right.Type == FieldType.Value ||
                    (requirement.Right.Type == FieldType.PreviousValue && requirement.Right.Value == requirement.Left.Value))
                {
                    string note;
                    if (notes.TryGetValue((int)requirement.Left.Value, out note))
                        Notes = note;
                }
                else
                {
                    var builder = new StringBuilder();

                    string note;
                    if (notes.TryGetValue((int)requirement.Left.Value, out note))
                        builder.AppendFormat("0x{0:x6}:{1}", requirement.Left.Value, note);

                    if (notes.TryGetValue((int)requirement.Right.Value, out note))
                    {
                        if (builder.Length > 0)
                            builder.AppendLine();
                        builder.AppendFormat("0x{0:x6}:{1}", requirement.Right.Value, note);
                    }

                    Notes = builder.ToString();
                }
            }
        }

        public RequirementViewModel(string definition, string notes)
        {
            Definition = definition;
            Notes = notes;
        }

        internal Requirement Requirement { get; private set; }

        public static readonly ModelProperty DefinitionProperty = ModelProperty.Register(typeof(RequirementViewModel), "Definition", typeof(string), "");
        public string Definition
        {
            get { return (string)GetValue(DefinitionProperty); }
            private set { SetValue(DefinitionProperty, value); }
        }


        public static readonly ModelProperty WrappedDefinitionProperty = 
            ModelProperty.RegisterDependant(typeof(RequirementViewModel), "WrappedDefinition", typeof(string), new[] { DefinitionProperty }, GetWrappedDefinition);

        public string WrappedDefinition
        {
            get { return (string)GetValue(WrappedDefinitionProperty); }
        }

        private static string GetWrappedDefinition(ModelBase model)
        {
            var definition = ((RequirementViewModel)model).Definition;

            if (definition.Length > 32)
            {
                var index = 32;
                while (!Char.IsWhiteSpace(definition[index]))
                    index--;

                if (index < 20)
                {
                    index = 32;
                    while (Char.IsLetterOrDigit(definition[index]))
                        index--;
                }

                definition = definition.Substring(0, index) + "\n     " + definition.Substring(index);
            }

            return definition;
        }        

        public string Notes { get; private set; }

        internal virtual void OnShowHexValuesChanged(ModelPropertyChangedEventArgs e)
        {
            if (Requirement != null)
                UpdateDefinition((bool)e.NewValue ? NumberFormat.Hexadecimal : NumberFormat.Decimal);
        }

        private void UpdateDefinition(NumberFormat numberFormat)
        {
            var builder = new StringBuilder();
            Requirement.AppendString(builder, numberFormat);
            Definition = builder.ToString();
        }
    }
}
