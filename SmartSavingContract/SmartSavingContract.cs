using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;

namespace SmartSavingContract
{
    public class SmartSavingContract : SmartContract
    {
        public static object Main(string operation, params object[] args)
        {
            switch (operation)
            {
                //This operation serves for creating new savings with zero balances.
                //It should receive name(string) as arg0 and duration(integer) as arg1.
                //Duration should be unix timestamp after which can savings be closed freely
                case "createSavings":
                    {
                        return "savingsId";
                    }
                //Returns json array of savings ids of all savings binded to caller address
                case "getAllSavings":
                    {
                        return "[]";
                    }
                //Returns json object with savings details.
                //Arg0 should be savingsId
                case "getSavings":
                    {
                        return null;
                    }
                //This method requires attached neo or gas which will be recorded os savings id
                //Both server(for reocurring payments) and client(for single non-planned payments)
                //will use this method
                //arg0 should be savingsId
                case "transfer":
                    {
                        return false;
                    }
                //This method only deletes savings data!
                //If current timestamp is greater than savings duration all assets will be transfered to
                //clients address. If savings is closed prematurely, some percentage will be kept on owner wallet.
                //Only server with contract owner address can close savings
                case "closeSavings":
                    {
                        return false;
                    }
            }
            return "unknown command";
        }
    }
}
