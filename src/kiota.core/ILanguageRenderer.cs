using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace kiota.core
{
    public interface ILanguageRenderer
    {
        void Render(RequestBuilder root, GenerationConfiguration config);
    }
}
