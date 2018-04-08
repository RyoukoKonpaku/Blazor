using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.AspNetCore.Blazor.Components
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class ClassNameElementAttribute : Attribute
    {
        public ClassNameElementAttribute()
        {
        }
    }
}
