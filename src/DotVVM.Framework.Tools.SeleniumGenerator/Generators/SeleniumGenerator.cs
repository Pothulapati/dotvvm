using System.Collections.Generic;
using System.Text;
using DotVVM.Framework.Binding;
using DotVVM.Framework.Compilation.ControlTree;
using DotVVM.Framework.Compilation.ControlTree.Resolved;
using DotVVM.Framework.Controls;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DotVVM.Framework.Tools.SeleniumGenerator.Generators
{
    public abstract class SeleniumGenerator<TControl> : ISeleniumGenerator where TControl : DotvvmControl
    {

        /// <summary>
        /// Gets a list of properties that can be used to determine the control name.
        /// </summary>
        public abstract DotvvmProperty[] NameProperties { get; }

        /// <summary>
        /// Gets a value indicating whether the content of the control can be used to generate the control name.
        /// </summary>
        public abstract bool CanUseControlContentForName { get; }


        /// <summary>
        /// Gets a list of declarations emitted by the control.
        /// </summary>
        public IEnumerable<MemberDeclarationSyntax> GetDeclarations(SeleniumGeneratorContext context)
        {
            // determine the name
            var uniqueName = DetermineName(context);

            // make the name unique
            if (context.UsedNames.Contains(uniqueName))
            {
                var index = 1;
                while (context.UsedNames.Contains(uniqueName + index))
                {
                    index++;
                }
            }
            context.UsedNames.Add(uniqueName);


            // determine the selector
            var selector = TryGetNameFromProperty(context.Control, UITests.SelectorProperty);
            if (selector == null)
            {
                selector = uniqueName;
                // TODO: update the markup
            }

            return GetDeclarationsCore(context);
        }


        protected virtual string DetermineName(SeleniumGeneratorContext context)
        {
            // get the name from the UITest.Name property
            var uniqueName = TryGetNameFromProperty(context.Control, UITests.NameProperty);

            // if not found, use the name properties to determine the name
            if (uniqueName == null)
            {
                foreach (var nameProperty in NameProperties)
                {
                    uniqueName = TryGetNameFromProperty(context.Control, nameProperty);
                    if (uniqueName != null)
                    {
                        break;
                    }
                }
            }

            // if not found, try to use the content of the control to determine the name
            if (uniqueName == null && CanUseControlContentForName)
            {
                uniqueName = GetTextFromContent(context.Control.Content);
            }

            // if not found, use control name
            if (uniqueName == null)
            {
                uniqueName = typeof(TControl).Name;
            }
            
            return uniqueName;
        }

        protected string TryGetNameFromProperty(ResolvedControl control, DotvvmProperty property)
        {
            IAbstractPropertySetter setter;
            if (control.TryGetProperty(UITests.NameProperty, out setter))
            {
                if (setter is ResolvedPropertyValue)
                {
                    return RemoveNonIdentifierCharacters(((ResolvedPropertyValue) setter).Value?.ToString());
                }
                else if (setter is ResolvedPropertyBinding)
                {
                    var binding = ((ResolvedPropertyBinding) setter).Binding;
                    return RemoveNonIdentifierCharacters(binding.Value);
                }
            }
            return null;
        }

        private string RemoveNonIdentifierCharacters(string value)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsLetterOrDigit(value[i]) || value[i] == '_')
                {
                    sb.Append(value[i]);
                }
            }

            if (sb.Length == 0)
            {
                return null;
            }
            else if (char.IsDigit(sb[0]))
            {
                sb.Insert(0, '_');
            }

            return sb.ToString();
        }

        private string GetTextFromContent(IEnumerable<ResolvedControl> controls)
        {
            var sb = new StringBuilder();

            foreach (var control in controls)
            {
                if (control.Metadata.Type == typeof(Literal))
                {
                    sb.Append(TryGetNameFromProperty(control, Literal.TextProperty));
                }
                else if (control.Metadata.Type == typeof(HtmlGenericControl))
                {
                    sb.Append(TryGetNameFromProperty(control, HtmlGenericControl.InnerTextProperty));
                }
            }

            // ensure the text is not too long
            var text = RemoveNonIdentifierCharacters(sb.ToString());
            if (text?.Length > 20)
            {
                text = text.Substring(0, 20);
            }
            return text;
        }


        protected abstract IEnumerable<MemberDeclarationSyntax> GetDeclarationsCore(SeleniumGeneratorContext context);

    }
}