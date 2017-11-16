using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.Numerics;
using System.Text;

namespace SmartSavingContract
{
    public class SmartSavingContract : SmartContract
    {

        private static readonly byte[] NEO = { 155, 124, 255, 218, 166, 116, 190, 174, 15, 147, 14, 190, 96, 133, 175, 144, 147, 229, 254, 86, 179, 74, 92, 34, 12, 205, 207, 110, 252, 51, 111, 197 };

        private static readonly byte[] NEO_GAS = { 223 };

        private static readonly byte[] DURATION = { 0x03 };

        public static object Main(string operation, params object[] args)
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
                        return CreateSavings((byte[]) args[0], (string) args[1],((byte[])args[2]).AsBigInteger());
                    }
                //Returns json array of savings ids of all savings binded to caller address
                case "getAllSavings":
                    {
                        Runtime.Log("getAllSavings command!");
                        return GetAllSavings((byte[])args[0]);
                    }
                //Returns json object with savings details.
                //Arg0 should be savingsId
                case "getSavingsByName":
                    {
                        Runtime.Log("getSavingsByName command!");
                        return GetSavingsByName((byte[])args[0], (string)args[1]);
                    }
                //This method requires attached neo or gas which will be recorded os savings id
                //Both server(for reocurring payments) and client(for single non-planned payments)
                //will use this method
                //arg0 should be savingsId
                case "transfer":
                    {
                        Runtime.Log("transfer command!");
                        return Transfer((byte[])args[0], (string) args[1]);
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

        public static bool CreateSavings(byte[] owner, string name, BigInteger duration)
        {
            Runtime.Log("Creating new savings...");
            if (!Runtime.CheckWitness(owner)) return false;
            BigInteger zero = 0;
            Runtime.Log("sender obtained");
            if (GetSavingsByName(owner, name) != null) {
                Runtime.Log("Savings with that name and awner already exists!");
                return false;
            }
            Runtime.Log("Savings with that name does not exists");
            string savings = GetAllSavings(owner);
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
            Storage.Put(Storage.CurrentContext, owner, savings);
            StoreNeoBalance(owner, name, zero);
            StoreNeoGasBalance(owner, name, zero);
            StoreDuration(owner, name, duration);
            return true;
        }

        public static string GetAllSavings(byte[] owner)
        {
            if (!Runtime.CheckWitness(owner)) return null;
            byte[] content = Storage.Get(Storage.CurrentContext, owner);
            if (content != null)
            {
                return Helper.AsString(content);
            }
            return null;
        }

        public static string GetSavingsByName(byte[] owner, string name)
        {
            Runtime.Log("getting savings in json");
            if (!Runtime.CheckWitness(owner)) return null;
            BigInteger duration = GetDuration(owner, name);
            if (duration == 0) return null;
            string savings = "{";
            savings += "\"name\":\"";
            savings += name;
            savings+= "\",";
            savings += "\"neo\":";
            Runtime.Log("fetching neo balance");
            BigInteger neoBalance = GetNeoBalance(owner, name);
            Runtime.Notify(neoBalance + 1);
            savings += neoBalance;
            savings += ",";
            savings += "\"gas\":";
            Runtime.Log("fetching gas balance");
            BigInteger neoGasBalance = GetNeoGasBalance(owner, name);
            Runtime.Notify(neoGasBalance + 1);
            Runtime.Log("gas: "+Helper.AsString(neoGasBalance.ToByteArray()));
            savings += neoGasBalance;
            savings += ",";
            savings += "\"duration\":";
            Runtime.Log("fetching duration");
            savings += duration;
            savings += ",";
            savings += "\"name\":\"";
            savings += Helper.AsString(owner);
            savings += "\"}";
            return savings;
        }

        public static bool Transfer(byte[] owner, string name)
        {
            if (!Runtime.CheckWitness(owner)) return false;
            BigInteger neoBalance = GetNeoBalance(owner, name);
            BigInteger neoContribution = GetNeoContributionValue();
            neoBalance = neoBalance + neoContribution;
            StoreNeoBalance(owner, name, neoBalance);
            BigInteger neoGasBalance = GetNeoBalance(owner, name);
            BigInteger neoGasContribution = GetNeoContributionValue();
            neoGasBalance = neoGasBalance + neoGasContribution;
            StoreNeoGasBalance(owner, name, neoGasBalance);
            return true;
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

        private static BigInteger GetNeoBalance(byte[] sender, string name)
        {
            byte[] data = GetData(sender, name, NEO);
            if (data == null) {
                return 0;
            }
            return data.AsBigInteger();
        }

        private static BigInteger GetNeoGasBalance(byte[] sender, string name)
        {
            byte[] data = GetData(sender, name, NEO_GAS);
            if (data == null)
            {
                return 0;
            }
            return data.AsBigInteger();
        }

        private static BigInteger GetDuration(byte[] sender, string name)
        {
            byte[] data = GetData(sender, name, DURATION);
            if (data == null)
            {
                return 0;
            }
            return data.AsBigInteger();
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

        private static byte[] GetData(byte[] sender, string name, byte[] assetId)
        {
            return Storage.Get(
                Storage.CurrentContext,
                Helper.Concat(
                    Helper.Concat(sender, Helper.AsByteArray(name)),
                    assetId
                    )
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

        private static ulong GetNeoContributionValue()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] outputs = tx.GetOutputs();
            ulong value = 0;
            foreach (TransactionOutput output in outputs)
            {
                if (output.ScriptHash == GetReceiver() && output.AssetId == NEO)
                {
                    value += (ulong)output.Value;
                }
                else
                {
                    Runtime.Log("Asset id: ");
                    Runtime.Log(Helper.AsString(output.AssetId));
                }
            }
            return value;
        }

        private static ulong GetNeoGasContributionValue()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] outputs = tx.GetOutputs();
            ulong value = 0;
            foreach (TransactionOutput output in outputs)
            {
                if (output.ScriptHash == GetReceiver() && output.AssetId != NEO)
                {
                    value += (ulong)output.Value;
                }
                else
                {
                    Runtime.Log("Asset id: ");
                    Runtime.Log(Helper.AsString(output.AssetId));
                }
            }
            return value;
        }

        // get smart contract script hash
        private static byte[] GetReceiver()
        {
            return ExecutionEngine.ExecutingScriptHash;
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
