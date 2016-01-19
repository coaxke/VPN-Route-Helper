using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VPNRouteHelper
{
    public class ValidatedMessage
    {
        public bool XSDValid { get; set; }
        public string ValidationMessage { get; set; }

        public ValidatedMessage(bool xsdvalid, string validationmessage)
        {
            XSDValid = xsdvalid;
            ValidationMessage = validationmessage;
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}