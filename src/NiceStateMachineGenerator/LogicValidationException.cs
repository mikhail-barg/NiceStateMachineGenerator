using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiceStateMachineGenerator
{
    public sealed class LogicValidationException : Exception
    {
        public readonly List<string>? Errors;

        public LogicValidationException()
        {
        }

        public LogicValidationException(string? message) 
            : base(message)
        {
        }

        public LogicValidationException(string? message, Exception? innerException) 
            : base(message, innerException)
        {
        }


        public LogicValidationException(List<string> errors)
            : base(errors.Count == 1? errors[0] : "Found multiple errors: " + String.Join("\n", errors))
        {
            this.Errors = errors;
        }
    }
}
