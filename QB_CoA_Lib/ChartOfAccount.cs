namespace QB_CoA_Lib
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
