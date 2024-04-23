using System;
using System.Collections.Generic;
using System.Linq;
namespace NPLTOOL.Parameter
{
    public interface IParameterProcessor
    {
        static string ProcessorType { get; }

        void SetValue(string key, object value);
    }
}
