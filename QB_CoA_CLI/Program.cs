using System.Data;
using System.Reflection.PortableExecutable;
using QB_CoA_Lib;

namespace coaaccounts
{
    public class Sample
    {
        public static void Main(string[] args)
        {
            CoAReader.QueryAllCoAs();
        }

    }
}