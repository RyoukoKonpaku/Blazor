// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    internal class ClassNameTagHelperDescriptorProvider : ITagHelperDescriptorProvider
    {
        // Run after the component tag helper provider, because we need to see the results.
        public int Order { get; set; } = 1000;

        public RazorEngine Engine { get; set; }

        public void Execute(TagHelperDescriptorProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var compilation = context.GetCompilation();
            if (compilation == null)
            {
                return;
            }

            // Tag Helper defintion for case #1. This is the most general case.
            context.Results.Add(CreateClassNameTagHelper());
        }

        private TagHelperDescriptor CreateClassNameTagHelper()
        {
            var builder = TagHelperDescriptorBuilder.Create(BlazorMetadata.ClassName.TagHelperKind, "Class", BlazorApi.AssemblyName);
            // TODO transfer to resources.
            builder.Documentation = "TODO Nyahha";

            builder.Metadata.Add(BlazorMetadata.SpecialKindKey, BlazorMetadata.ClassName.TagHelperKind);
            builder.Metadata[TagHelperMetadata.Runtime.Name] = BlazorMetadata.ClassName.RuntimeName;

            // WTE has a bug in 15.7p1 where a Tag Helper without a display-name that looks like
            // a C# property will crash trying to create the toolips.
            builder.SetTypeName("Microsoft.AspNetCore.Blazor.Components.ClassName");

            builder.TagMatchingRule(rule =>
            {
                rule.TagName = "*";
                rule.Attribute(attribute =>
                {
                    attribute.Name = "class.";
                    attribute.NameComparisonMode = RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch;
                });
            });

            builder.BindAttribute(attribute =>
            {
                attribute.Documentation = Resources.BindTagHelper_Fallback_Documentation;

                attribute.Name = "class.";
                attribute.AsDictionary("class.", typeof(bool).FullName);

                // WTE has a bug 15.7p1 where a Tag Helper without a display-name that looks like
                // a C# property will crash trying to create the toolips.
                attribute.SetPropertyName("Class");
                attribute.TypeName = "System.Collections.Generic.Dictionary<string, bool>";
            });

            return builder.Build();
        }
    }
}