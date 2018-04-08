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
    internal class ConditionalClassTagHelperDescriptorProvider : ITagHelperDescriptorProvider
    {
        // I don't know if this is necessary here.
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
            
            context.Results.Add(ConditionalClassTagHelper());
        }

        private TagHelperDescriptor ConditionalClassTagHelper()
        {
            var builder = TagHelperDescriptorBuilder.Create(BlazorMetadata.ConditionalClass.TagHelperKind, "Class", BlazorApi.AssemblyName);
            builder.Documentation = Resources.ConditionalClassTagHelper_Documentation;

            builder.Metadata.Add(BlazorMetadata.SpecialKindKey, BlazorMetadata.ConditionalClass.TagHelperKind);
            builder.Metadata[TagHelperMetadata.Runtime.Name] = BlazorMetadata.ConditionalClass.RuntimeName;

            // WTE has a bug in 15.7p1 where a Tag Helper without a display-name that looks like
            // a C# property will crash trying to create the toolips.
            builder.SetTypeName("Microsoft.AspNetCore.Blazor.Components.ConditionalClass");

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