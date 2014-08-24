using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Molly
{
    public class DetourManager : IDisposable
    {
        public DetourManager()
        {

        }
        ~DetourManager()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {

            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
