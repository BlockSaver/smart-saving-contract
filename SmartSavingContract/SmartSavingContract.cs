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

        //neo asset id
        private static readonly byte[] NEO = { 155, 124, 255, 218, 166, 116, 190, 174, 15, 147, 14, 190, 96, 133, 175, 144, 147, 229, 254, 86, 179, 74, 92, 34, 12, 205, 207, 110, 252, 51, 111, 197 };

        //neo gas id
        private static readonly byte[] NEO_GAS = { 231, 45, 40, 105, 121, 238, 108, 177, 183, 230, 93, 253, 223, 178, 227, 132, 16, 11, 141, 20, 142, 119, 88, 222, 66, 228, 22, 139, 113, 121, 44, 96 };

        //contract owner public key
        private static readonly byte[] CONTRACT_OWNER = { 108, 134, 156, 63, 118, 253, 80, 65, 30, 70, 152, 182, 20, 222, 126, 201, 17, 14, 146, 101, 170, 211, 90, 227, 16, 54, 140, 24, 115, 6, 78, 148, 2 };

        //savings duration id
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
                        Runtime.Log("closeSavings command!");
                        return CloseSavings((byte[])args[0], (string)args[1], (byte[]) args[2]);
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
            byte[] content = Storage.Get(Storage.CurrentContext, owner);
            if (content != null)
            {
                string savings = Helper.AsString(content);
                Runtime.Log("Savings: " + savings);
                return savings;
            }
            return null;
        }

        public static string GetSavingsByName(byte[] owner, string name)
        {
            Runtime.Log("getting savings in json");
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

        public static bool CloseSavings(byte[] savingsOwner, string name, byte[] signature)
        {
            //this is commented out because we aren't really sure how to generate signature in neo-gui
            //we also realize that currently this is security flaw
            //if (!VerifySignature(signature, CONTRACT_OWNER)) return false;
            Runtime.Log("Closing savings ->" + name + "...");
            string savings = GetAllSavings(savingsOwner);
            Runtime.Log("Current savings" + savings);
            byte[] newSavings = RemoveFromArray(savings.AsByteArray(), name);
            Runtime.Log("Removed from savings list");
            Storage.Put(Storage.CurrentContext, savingsOwner, newSavings);
            Runtime.Log("Stored new savings list");
            StoreNeoBalance(savingsOwner, name, 0);
            Runtime.Log("Stored new neo balance");
            StoreNeoGasBalance(savingsOwner, name, 0);
            Runtime.Log("Stored new neo gas balance");
            StoreDuration(savingsOwner, name, 0);
            Runtime.Log("Stored new duration");
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
            return GetAssetContribution(NEO);
        }

        private static ulong GetNeoGasContributionValue()
        {
            return GetAssetContribution(NEO_GAS);
        }

        private static ulong GetAssetContribution(byte[] assetId)
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] outputs = tx.GetOutputs();
            ulong value = 0;
            foreach (TransactionOutput output in outputs)
            {
                if (output.ScriptHash == GetReceiver() && output.AssetId == assetId)
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

        private static byte[] RemoveFromArray(byte[] source, string needle) {
            if (source.Equals(needle.AsByteArray())) {
                return null;
            }
            string needle2 = needle + ",";
            byte[] pattern = needle.AsByteArray();
            byte[] pattern2 = needle2.AsByteArray();
            byte[] result = new byte[0];
            for (int i = 0; i < source.Length; i++) {
                byte[] part = Helper.Range(source, i, pattern2.Length);
                if (part.Equals(pattern2)) {
                    result = Helper.Concat(result, takeTillEnd(source, i + pattern2.Length));
                    return result;
                }
                part = Helper.Range(source, i, pattern.Length);
                if (part.Equals(pattern))
                {
                    result = Helper.Concat(result, takeTillEnd(source, i + pattern.Length));
                    return result;
                }
                result = Helper.Concat(result, new byte[] { source[i] });
            }
            return result;
        }

        private static byte[] takeTillEnd(byte[] source, int index) {
            return Helper.Range(source, index, source.Length - index);
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
