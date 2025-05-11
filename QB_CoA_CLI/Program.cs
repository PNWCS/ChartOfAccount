using System.ComponentModel.Design;
using System.Data;
using System.Reflection.PortableExecutable;
using QB_CoA_Lib;
using ClosedXML.Excel;

namespace coaaccounts
{
    public class Sample
    {

        public static void Main(string[] args)


        {
            LoggerConfig.ConfigureLogging();
            string filePath = "..\\..\\..\\..\\..\\Sample_Company_Data.xlsx";
            List<ChartOfAccount> companyTerms = new List<ChartOfAccount>();

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"The file '{filePath}' does not exist.");

            var sampleAccounts = new List<ChartOfAccount>
        {
            new ChartOfAccount("Bank", "1000", "Sarma") { CompanyID = "COMP001" },
            new ChartOfAccount("Bank", "6000", "Narendra Kumar Tokala") { CompanyID = "COMP001" }

        };

            Console.WriteLine("Read or Write or Compare");

            String? a = Console.ReadLine();

            if (a.ToLower() == "read") { CoAReader.QueryAllCoAs(); }

            if (a.ToLower() == "compare")
            {

                using (var workbook = new XLWorkbook(filePath))
                {
                    var worksheet = workbook.Worksheet("chartofaccount");

                    // Get the range of used rows

                    var range = worksheet.RangeUsed();
                    if (range == null)
                    {
                        Console.WriteLine("Warning: The worksheet is empty or contains no used range.");
                    }
                    else
                    {
                        var rows = range.RowsUsed();
                        foreach (var row in rows.Skip(1)) // Skip header row
                        {
                            string accountType = row.Cell(1).GetString().Trim();  // Column "Name"
                            string accountnumber = row.Cell(2).GetString().Trim();
                            string name = row.Cell(3).GetString().Trim();
                            string id = row.Cell(4).GetString().Trim();


                            companyTerms.Add(new ChartOfAccount(accountType, accountnumber, name)
                            {

                                CompanyID = id

                                // Set the QB_ID
                            });
                        }
                    }
                }

                List<ChartOfAccount> terms = CoAComparator.CompareAccounts(companyTerms);
                foreach (var term in terms)
                {
                    Console.WriteLine($"Term {term.Name} has the {term.Status} Status");
                }

                Console.WriteLine("Data Sync Completed");


            }

            else { CoAAdder.AddCustomers(sampleAccounts); }









            // 
        }

    }
}