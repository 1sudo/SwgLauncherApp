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
            CipherHandler cipher = new();
            string user = cipher.Encode(await cipher.Transform(username.ToLower()));
            string pass = cipher.Encode(await cipher.Transform(password));

            List<DatabaseProperties.Accounts> searchedAccount = await ExecuteAccountAsync
                (
                    "SELECT Username " +
                    "FROM Accounts " +
                    $"where Username = '{user.ToLower()}';"
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
                        $"SET Username = '{user}', Password = '{pass}' " +
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
                        $"('{user}', '{pass}');"
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

            CipherHandler cipher = new();

            if (accounts.Count > 0)
            {
                return new Dictionary<string, string>
                {
                    { "Username", await cipher.Transform(cipher.Decode(accounts[0].Username)) },
                    { "Password", await cipher.Transform(cipher.Decode(accounts[0].Password)) }
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
