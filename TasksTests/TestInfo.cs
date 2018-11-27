using System;
using System.Collections.Generic;
using System.Text;


namespace Tasks
{
    [AttributeUsage(
   AttributeTargets.Method |
   AttributeTargets.Property,
   AllowMultiple = true)]
    public class TestInfo : System.Attribute
    {
        private string _precondition;
        private string _Description;
        private string _ExpectedResult;

        public TestInfo(string precondition, string Description, string ExpectedResult)
        {
            _precondition = precondition ?? throw new ArgumentNullException(nameof(precondition));
            _Description = Description ?? throw new ArgumentNullException(nameof(Description));
            _ExpectedResult = ExpectedResult ?? throw new ArgumentNullException(nameof(ExpectedResult));
        }


    }
}
