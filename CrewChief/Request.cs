using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChief
{
    interface Request
    {
        /**
         * Pass in the classname of the event which will handle the response
         */
        void requestResponse(String responseClassName);
    }
}
