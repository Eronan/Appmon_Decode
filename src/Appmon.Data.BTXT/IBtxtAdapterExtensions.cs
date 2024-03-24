namespace Appmon.Data.BTXT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class IBtxtAdapterExtensions
{
    public static IBtxtAdapter GetDefaultBtxtAdapter()
    {
        return new BtxtAdapter();
    }
}
