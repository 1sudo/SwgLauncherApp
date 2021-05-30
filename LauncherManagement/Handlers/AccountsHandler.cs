using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace LauncherManagement
{
    public class AccountsHandler : DatabaseHandler
    {
        public async Task SaveCredentialsAsync(string username, string password)
        {
            List<DatabaseProperties.Accounts> searchedAccount = await ExecuteAccountAsync
                (
                    "SELECT Username " +
                    "FROM Accounts " +
                    $"where Username = '{username.ToLower()}';"
                );

            List<DatabaseProperties.Accounts> totalAccounts = await ExecuteAccountAsync
                (
                    "SELECT * " +
                    "FROM Accounts;"
                );


            if (searchedAccount.Count > 0)
            {
                return;
            }
            else if (totalAccounts.Count > 0)
            {
                await ExecuteAccountAsync
                    (
                        "UPDATE Accounts " +
                        $"SET Username = '{username.ToLower()}', Password = '{password}' " +
                        "WHERE Id = 1;"
                    );
            }
            else
            {
                await ExecuteAccountAsync
                    (
                        "INSERT into Accounts " +
                        "(Username, Password) " +
                        "VALUES " +
                        $"('{username.ToLower()}', '{password}');"
                    );
            }
        }

        public async Task<Dictionary<string, string>> GetAccountCredentialsAsync()
        {    
            List<DatabaseProperties.Accounts> accounts = await ExecuteAccountAsync
                (
                    "SELECT Username, Password " +
                    "FROM Accounts " +
                    "where Id = 1;"
                );

            if (accounts.Count > 0)
            {
                return new Dictionary<string, string>
                {
                    { "Username", accounts[0].Username },
                    { "Password", accounts[0].Password }
                };
            }
            else
            {
                return new Dictionary<string, string>
                {
                    { "Username", "" },
                    { "Password", "" }
                };
            }
        }
    }
}
