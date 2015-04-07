using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KiwiBoard.BL
{
    internal sealed class DataProvider
    {
        static DataProvider()
        {
            KiwiBoardEntities = new KiwiBoardEntities();
        }
        public static KiwiBoardEntities KiwiBoardEntities { get; private set; }
    }
}