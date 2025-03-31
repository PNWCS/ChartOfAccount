//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace QB_Terms_Lib
//{
//    public class PaymentTerm
//    {
//        public string QB_ID { get; set; }
//        public string QB_Rev { get; set; }
//        public string Name { get; set; }
//        public int Company_ID { get; set; }

//        public PaymentTerm(string qbID, string qbRev, string name, int companyID)
//        {
//            QB_ID = qbID;
//            QB_Rev = qbRev;
//            Name = name;
//            Company_ID = companyID;
//        }

//    }
//}
namespace QB_Terms_Lib
{
    public class ChartOfAccount
    {
        public string AccountType { get; set; }
        public string AccountNumber { get; set; }
        public string Name { get; set; }
        public string? CompanyID { get; set; }
        public string QB_ID { get; set; }

        public ChartOfAccount(string accountType, string accountNumber, string name)
        {
            AccountType = accountType;
            AccountNumber = accountNumber;
            Name = name;


        }
    }
}
