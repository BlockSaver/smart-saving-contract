using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.Numerics;

namespace SmartSavingContract
{
    public class SmartSavingContract : SmartContract
    {

        private static readonly byte[] NEO = { 0x01 };

        private static readonly byte[] NEO_GAS = { 0x02 };

        private static readonly byte[] DURATION = { 0x03 };

        public static object Main(string operation, string name, BigInteger duration)
        {
            Runtime.Log("operation: "+operation);
            switch (operation)
            {
                //This operation serves for creating new savings with zero balances.
                //It should receive name(string) as arg0 and duration(integer) as arg1.
                //Duration should be unix timestamp after which can savings be closed freely
                //It will return true is savings is created successfully, false otherwise
                case "createSavings":
                    {
                        Runtime.Log("createSavings command!");
                        return CreateSavings(name, duration);
                    }
                //Returns json array of savings ids of all savings binded to caller address
                case "getAllSavings":
                    {
                        Runtime.Log("getAllSavings command!");
                        return GetAllSavings();
                    }
                //Returns json object with savings details.
                //Arg0 should be savingsId
                case "getSavingsByName":
                    {
                        Runtime.Log("getSavingsByName command!");
                        return GetSavingsByName(name);
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

        /**
         * Operations
         * */

        public static bool CreateSavings(string name, BigInteger duration)
        {
            Runtime.Log("Creating new savings...");
            BigInteger zero = 0;
            Runtime.Log("integer init");
            byte[] sender = ExecutionEngine.CallingScriptHash;
            Runtime.Log("sender obtained");
            if (GetSavingsByName(name) != null) {
                Runtime.Log("Savings with that name and awner already exists!");
                return false;
            }
            Runtime.Log("Savings with that name does not exists");
            string savings = GetAllSavings();
            if (savings != null)
            {
                Runtime.Log("all savings:");
                Runtime.Log(savings);
                savings += ",";
                savings += name;
            }
            else {
                savings = name;
            }
            Storage.Put(Storage.CurrentContext, sender, savings);
            StoreNeoBalance(sender, name, zero);
            StoreNeoGasBalance(sender, name, zero);
            StoreDuration(sender, name, duration);
            return true;
        }

        public static string GetAllSavings()
        {
            byte[] sender = ExecutionEngine.CallingScriptHash;
            byte[] content = Storage.Get(Storage.CurrentContext, sender);
            if (content != null)
            {
                return Helper.AsString(content);
            }
            return null;
        }

        public static string GetSavingsByName(string name)
        {
            Runtime.Log("serializing savings to json");
            string savings = "{";
            savings += "\"name\":\"";
            savings += name;
            savings+= "\",";
            savings += "\"neo\":";
            savings += "0";
            savings += ",";
            savings += "\"gas\":";
            savings += "0";
            savings += ",";
            savings += "\"duration\":";
            savings += "0";
            savings += "}";
            return savings;
        }

        /**
         * Utility methods
         * */
        private static void StoreNeoBalance(byte[] sender, string name, BigInteger value)
        {
            StoreData(sender, name, NEO, value.AsByteArray());
        }

        private static void StoreNeoGasBalance(byte[] sender, string name, BigInteger value)
        {
            StoreData(sender, name, NEO_GAS, value.AsByteArray());
        }

        private static void StoreDuration(byte[] sender, string name, BigInteger value)
        {
            StoreData(sender, name, DURATION, value.AsByteArray());
        }

        private static void StoreData(byte[] sender, string name, byte[] assetId, byte[] data)
        {
            Storage.Put(
                Storage.CurrentContext,
                Helper.Concat(
                    Helper.Concat(sender, Helper.AsByteArray(name)),
                    assetId
                    ),
                data
                );
        }


        //Get the sender's public key
        private static byte[] GetSender()
        {
            Runtime.Log("Fetching transaction");
            Transaction tx = (Transaction) ExecutionEngine.ScriptContainer;
            Runtime.Log("Fetched transaction");
            TransactionOutput[] reference = tx.GetReferences();
            Runtime.Log("reference");
            TransactionOutput firstReference = reference[0];
            Runtime.Log("first reference");
            return firstReference.ScriptHash;
        }

        private static bool ArrayContains(string[] array, string needle)
        {
            foreach (string i in array) {
                if (i.Equals(needle)) {
                    return true;
                }
            }
            return false;
        }
    }
}
