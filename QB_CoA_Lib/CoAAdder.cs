using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using QBFC16Lib;
using Serilog;


namespace QB_CoA_Lib
{
    public class CoAAdder
    {
        //public List<ChartOfAccount> accounts = new List<ChartOfAccount>();
        static CoAAdder()
        {

            LoggerConfig.ConfigureLogging(); // Safe to call (only initializes once)
            Log.Information("CoAAdder Initialized.");


        }




        public static void AddCustomers(List<ChartOfAccount> accounts)
        {
            using (var qbSession = new QuickBooksSession(AppConfig.QB_APP_NAME))
            {

                foreach (var acct in accounts)
                {
                    string qbID = AddAccount(
                        qbSession,
                        acct.Name,
                        acct.AccountType,
                        acct.AccountNumber,
                        acct.CompanyID
                    );
                    acct.QB_ID = qbID; // Store the returned QB ListID.

                }



            }
            Log.Information("CoAAdder Completed");



        }

        private static string AddAccount(
            QuickBooksSession qbSession,
            string name,
            string accountType,
            string accountNumber,
            string companyID
        )
        {
            IMsgSetRequest requestMsgSet = qbSession.CreateRequestSet();
            IAccountAdd accountAddRq = requestMsgSet.AppendAccountAddRq();

            accountAddRq.Name.SetValue(name);
            accountAddRq.AccountType.SetValue(ConvertStringToAccountType(accountType));
            accountAddRq.AccountNumber.SetValue(accountNumber);
            accountAddRq.Desc.SetValue(companyID);

            IMsgSetResponse responseMsgSet = qbSession.SendRequest(requestMsgSet);

            return ExtractListIDFromResponse(responseMsgSet);
        }
        private static ENAccountType ConvertStringToAccountType(string accountType)
        {
            return accountType.ToLower() switch
            {
                "bank" => ENAccountType.atBank,
                "expense" => ENAccountType.atExpense,
                "income" => ENAccountType.atIncome,
                _ => ENAccountType.atOtherAsset
            };
        }

        private static string ExtractListIDFromResponse(IMsgSetResponse responseMsgSet)
        {
            IResponseList responseList = responseMsgSet.ResponseList;
            if (responseList == null || responseList.Count == 0)
                throw new Exception("No response from AccountAddRq.");

            IResponse response = responseList.GetAt(0);
            if (response.StatusCode != 0)
                throw new Exception($"AccountAdd failed: {response.StatusMessage}");

            IAccountRet? accountRet = response.Detail as IAccountRet;
            if (accountRet == null)
                throw new Exception("No IAccountRet returned after adding Account.");

            return accountRet.ListID?.GetValue()
                ?? throw new Exception("ListID is missing in QuickBooks response.");

        }


    }
}
