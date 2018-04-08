using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    internal class ConditionalClassLoweringPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        // Run after event handler pass
        public override int Order => base.Order + 50;

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var @namespace = documentNode.FindPrimaryNamespace();
            var @class = documentNode.FindPrimaryClass();
            if (@namespace == null || @class == null)
            {
                // Nothing to do, bail. We can't function without the standard structure.
                return;
            }

            // For each class. usage rewrite to a ternary operator and append to class attribute.
            var nodes = documentNode.FindDescendantNodes<TagHelperIntermediateNode>();
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];

                // Determine if the element has a class attribute already.
                int? classIndex = null;
                var updateClassIndex = true;
                for (var j = node.Children.Count - 1; j >= 0; j--)
                {
                    if (updateClassIndex)
                    {
                        for (var c = node.Children.Count - 1; c >= 0; c--)
                        {
                            // The HtmlAttributeNode of the original class attribute would be converted to
                            // a ComponentAttributeExtensionNode after the usage is already rerwitten once
                            // so we should check for both cases.
                            // There might be a better way of handling this though.
                            if ((node.Children[c] is TagHelperHtmlAttributeIntermediateNode classNode0 &&
                                classNode0.AttributeName.Equals("class")) ||
                                (node.Children[c] is ComponentAttributeExtensionNode classNode1 &&
                                classNode1.AttributeName.Equals("class")))
                            {
                                // If class is found bail out of the loop as there should be
                                // only one class attribute per element.
                                classIndex = c;
                                updateClassIndex = false;
                                break;
                            }
                        }
                    }

                    if (node.Children[j] is ComponentAttributeExtensionNode attributeNode &&
                        attributeNode.TagHelper != null &&
                        attributeNode.TagHelper.IsConditionalClassTagHelper() &&
                        attributeNode.AttributeName.StartsWith("class."))
                    {
                        RewriteUsage(node, j, attributeNode, classIndex);
                        updateClassIndex = true;
                    }
                }
            }
        }

        private void RewriteUsage(TagHelperIntermediateNode node, int index, ComponentAttributeExtensionNode attributeNode, int? htmlClassIndex)
        {
            // The class usage is like how Bind-... works wherein it rewrites the tag helper into a ternary operator 
            // that is appended to the class attribute.
            // so this code
            // <div class.highlight=@(5 > 1) class="class1"></div>
            // would be rewritten as 
            // <div class="class1 @(5 > 1 ? "highlight" : string.Empty)"></div>

            var original = GetAttributeContent(attributeNode);
            if (string.IsNullOrEmpty(original.Content))
            {
                // This can happen in error cases, the parser will already have flagged this
                // as an error, so ignore it.
                return;
            }


            // Now rewrite the content of the class node
            // Only get the segments after class. onwards
            // a dot after class. is treated as a new class
            var classes = string.Join(" ", attributeNode.AttributeName.Substring(6).Split('.'));

            var expression = new CSharpExpressionIntermediateNode();
            var classAttribute = new ComponentAttributeExtensionNode(attributeNode)
            {
                AttributeName = "class"
            };
            classAttribute.Children.Clear();
            classAttribute.Children.Add(expression);

            // If class is already existing append the operators.
            if (htmlClassIndex != null)
            {
                var classContent = GetAttributeContent(node.Children[htmlClassIndex.Value]);
                expression.Children.Add(new IntermediateToken()
                {
                    Content = $@"{classContent.Content} + "" "" + ({original.Content} ? ""{classes}"" : string.Empty)",
                    Kind = TokenKind.CSharp
                });
            }
            else
            {
                expression.Children.Add(new IntermediateToken()
                {
                    Content = $@"({original.Content} ? ""{classes}"" : string.Empty)",
                    Kind = TokenKind.CSharp
                });
            }

            // If a class attribute is already existing, 
            // append the generated ternary operator to the class string.
            if (htmlClassIndex != null)
            {
                node.Children[htmlClassIndex.Value] = classAttribute;
            }
            else
            {
                node.Children.Add(classAttribute);
            }

            // We don't need this node anymore
            node.Children.RemoveAt(index);
        }

        private static IntermediateToken GetAttributeContent(IntermediateNode node)
        {
            if (node.Children[0] is HtmlContentIntermediateNode htmlContentNode)
            {
                // This case can be hit for a 'string' attribute. We want to turn it into
                // an expression.
                var content = "\"" + ((IntermediateToken)htmlContentNode.Children.Single()).Content + "\"";
                return new IntermediateToken() { Kind = TokenKind.CSharp, Content = content };
            }
            else if (node.Children[0] is CSharpExpressionIntermediateNode cSharpNode)
            {
                // This case can be hit when the attribute has an explicit @ inside, which
                // 'escapes' any special sugar we provide for codegen.
                return ((IntermediateToken)cSharpNode.Children.Single());
            }
            else
            {
                // This is the common case for 'mixed' content
                return ((IntermediateToken)node.Children.Single());
            }
        }
    }
}
