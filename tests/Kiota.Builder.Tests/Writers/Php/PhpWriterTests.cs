using System;
using System.IO;

namespace Kiota.Builder.Tests.Writers.Php
{
    public class PhpWriterTests: IDisposable
    {
        private StringWriter tw;
        public void Dispose()
        {
            tw?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
