using QBFC16Lib;
using Serilog;
using System;
using System.Collections.Generic;


namespace QB_CoA_Lib
{
    public class CoAReader

    {
        static CoAReader()
        {
            LoggerConfig.ConfigureLogging(); // Safe to call (only initializes once)
            Log.Information("ChartOfAccountReader Initialized.");
        }
        public static List<ChartOfAccount> QueryAllCoAs()
        {
            bool sessionBegun = false;
            bool connectionOpen = false;
            QBSessionManager sessionManager = null;
            List<ChartOfAccount> accounts = new List<ChartOfAccount>();

            try
            {
                // Create the session Manager object
                sessionManager = new QBSessionManager();

                // Create the message set request object to hold our request
                IMsgSetRequest requestMsgSet = sessionManager.CreateMsgSetRequest("US", 16, 0);
                requestMsgSet.Attributes.OnError = ENRqOnError.roeContinue;

                // Append AccountQuery request
                IAccountQuery AccountQueryRq = requestMsgSet.AppendAccountQueryRq();

                // Connect to QuickBooks and begin a session
                sessionManager.OpenConnection("", AppConfig.QB_APP_NAME);
                connectionOpen = true;
                sessionManager.BeginSession("", ENOpenMode.omDontCare);
                sessionBegun = true;

                // Send the request and get the response from QuickBooks
                IMsgSetResponse responseMsgSet = sessionManager.DoRequests(requestMsgSet);

                // End the session and close the connection to QuickBooks
                sessionManager.EndSession();
                sessionBegun = false;
                sessionManager.CloseConnection();
                connectionOpen = false;

                accounts = WalkAccountQueryRs(responseMsgSet);
                Log.Information("ChartOfAccountReader Completed.");
                return accounts;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                if (sessionBegun)
                {
                    sessionManager.EndSession();
                }
                if (connectionOpen)
                {
                    sessionManager.CloseConnection();
                }
                return accounts;
            }

        }

        static List<ChartOfAccount> WalkAccountQueryRs(IMsgSetResponse responseMsgSet)
        {
            List<ChartOfAccount> accounts = new List<ChartOfAccount>();
            if (responseMsgSet == null) return accounts;
            IResponseList responseList = responseMsgSet.ResponseList;
            if (responseList == null) return accounts;

            for (int i = 0; i < responseList.Count; i++)
            {
                IResponse response = responseList.GetAt(i);
                if (response.StatusCode >= 0 && response.Detail != null)
                {
                    ENResponseType responseType = (ENResponseType)response.Type.GetValue();
                    if (responseType == ENResponseType.rtAccountQueryRs)
                    {
                        IAccountRetList AccountRetList = (IAccountRetList)response.Detail;
                        accounts = WalkAccountRet(AccountRetList);
                    }
                }
            }
            return accounts;
        }

        static List<ChartOfAccount> WalkAccountRet(IAccountRetList AccountRetList)
        {
            List<ChartOfAccount> accounts = new List<ChartOfAccount>();
            if (AccountRetList == null) return accounts;
            Log.Information($"account type \t \t \t account number \t \t \t name");
            for (int i = 0; i < AccountRetList.Count; i++)
            {
                var account = AccountRetList.GetAt(i);
                string accountType = account.AccountType.GetValue().ToString();
                if (accountType == "atBank")
                {
                    accountType = "Bank";

                }
                else if (accountType == "atExpense")
                {
                    accountType = "Expense";
                }
                else
                {
                    accountType = "Income";
                }
                string accountNumber = account.AccountNumber?.GetValue() ?? "N/A";
                var qb_id = account.ListID.GetValue();
                string name = account.Name.GetValue();
                string? desc = account.Desc?.GetValue();
                Log.Information($"Successfully retrieved {name} from QB");
                Log.Debug($"{accountType} \t \t \t {accountNumber} \t \t \t {name}");
                var chartOfAccount = new ChartOfAccount(accountType, accountNumber, name)
                {
                    QB_ID = qb_id,
                    CompanyID = desc

                    // Set the QB_ID
                };


                accounts.Add(chartOfAccount);
            }

            return accounts;
        }
    }
}
