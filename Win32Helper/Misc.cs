using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Win32Helper
{
    public static class Misc
    {
        public static void DebugBox(string text, string caption = "")
        {
            Imports.MessageBox(IntPtr.Zero, text, caption, 0);
        }
    }
}
