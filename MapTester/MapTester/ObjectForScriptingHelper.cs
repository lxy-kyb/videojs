using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapTester
{
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public class ObjectForScriptingHelper
    {
        MainWindow mainWindow;
        public ObjectForScriptingHelper(MainWindow main)
        {
            mainWindow = main;
        }

    }
}
