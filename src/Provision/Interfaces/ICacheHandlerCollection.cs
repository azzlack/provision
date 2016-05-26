using System.Collections.Generic;

namespace Provision.Interfaces
{
    public interface ICacheHandlerCollection : IList<ICacheHandler>, ICacheProvider
    {   
    }
}