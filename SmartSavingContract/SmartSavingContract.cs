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
                case "createSavings":
                    {
                        return "savingsId";
                    }
                case "getAllSavings":
                    {
                        return "[]";
                    }
                case "getSavings":
                    {
                        return null;
                    }
                case "transfer":
                    {
                        return false;
                    }
                case "closeSavings":
                    {
                        return false;
                    }
            }
            return "unknown command";
        }
    }
}
