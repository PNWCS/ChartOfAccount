using System.ComponentModel.Design;
using System.Data;
using System.Reflection.PortableExecutable;
using QB_CoA_Lib;

namespace coaaccounts
{
    public class Sample
    {

        public static void Main(string[] args)


        {
            var sampleAccounts = new List<ChartOfAccount>
        {
            new ChartOfAccount("Bank", "1000", "Sarma") { CompanyID = "COMP001" },
            new ChartOfAccount("Bank", "6000", "Narendra Kumar Tokala") { CompanyID = "COMP001" }

        };

            Console.WriteLine("Read or Write");

            String? a = Console.ReadLine();

            if (a.ToLower() == "read") { CoAReader.QueryAllCoAs(); }

            else { CoAAdder.AddCustomers(sampleAccounts); }







            // 
        }

    }
}