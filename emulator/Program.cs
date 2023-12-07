    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    //Thread pool
    class Program
    {
        private static readonly object lockObject = new object();
        private static DebitCardState cardState = new DebitCardState();
        static async Task Main(string[] args)
        {
            string inputFolder = "/Users/tim/Projects/emulator/emulator/input";
            string outputFolder = "/Users/tim/Projects/emulator/emulator/output";

            var csvFiles = Directory.GetFiles(inputFolder, "*.csv")
                                    .OrderBy(file => file)
                                    .ToList();

            DebitCardState cardState = new DebitCardState();

            foreach (var csvFile in csvFiles)
            {
                ThreadPool.QueueUserWorkItem(state => ProcessFile(csvFile, cardState, outputFolder));
            }

            while (ThreadPool.PendingWorkItemCount > 0)
             {
            await Task.Delay(100);
             }

        ResultSummary resultSummary = new ResultSummary
            {
                FinalBalance = cardState.Balance,
                SuccessfulTransactions = cardState.SuccessfulTransactions,
                UnsuccessfulTransactions = cardState.UnsuccessfulTransactions,
                TotalFilesProcessed = csvFiles.Count
            };

            File.WriteAllText(Path.Combine(outputFolder, "result.json"), resultSummary.ToJson());
        }

        static void ProcessFile(string file, DebitCardState cardState, string outputFolder)
        {
            string filePath = file;
            try
            {
               
                    var lines = File.ReadAllLines(filePath);

                    foreach (var line in lines)
                    {
                        var parts = line.Split(',');

                        if (parts.Length == 3)
                        {
                            var dateStr = parts[0];
                            var typeStr = parts[1];
                            var amountStr = parts[2];

                            if (DateTime.TryParse(dateStr, out DateTime date) &&
                                Enum.TryParse<TransactionType>(typeStr, true, out TransactionType transactionType) &&
                                decimal.TryParse(amountStr, NumberStyles.Currency, CultureInfo.InvariantCulture, out decimal amount))
                            {
                                var transaction = new Transaction
                                {
                                    Date = date,
                                    Type = transactionType,
                                    Amount = amount
                                };

                                ProcessTransaction(transaction, cardState, outputFolder);
                            }
                            else
                            {
                                LogError(filePath, "Invalid format in CSV line: " + line);
                            }
                        }
                        else
                        {
                            LogError(filePath, "Invalid CSV line: " + line);
                        }
                    
                }
            }
            catch (Exception ex)
            {
                LogError(filePath, "Error reading CSV file: " + ex.Message);
            }
        }






    static void ProcessTransaction(Transaction transaction, DebitCardState cardState, string outputFolder)
        {
            
                if (transaction.Type == TransactionType.Deposit)
                {
                    cardState.UpdateBalance(transaction.Amount);
                    cardState.AddSuccessfulTransaction(transaction);
                    LogTransaction(outputFolder, transaction, "Success");
                }
                else if (transaction.Type == TransactionType.Withdrawal)
                {
                    if (cardState.CanWithdraw(transaction.Amount))
                    {
                        cardState.UpdateBalance(-transaction.Amount);
                        cardState.AddSuccessfulTransaction(transaction);
                        LogTransaction(outputFolder, transaction, "Success");
                    }
                    else
                    {
                        cardState.AddUnsuccessfulTransaction(transaction);
                        LogTransaction(outputFolder, transaction, "Failed (Insufficient funds)");
                    }
                }
            
        }

        static void LogTransaction(string outputFolder, Transaction transaction, string status)
        {
            string logPath = Path.Combine(outputFolder, "transaction_log.csv");
            string logEntry = $"{transaction.Date},{transaction.Type},{transaction.Amount},{status}\n";
        lock (lockObject)
        {
            File.AppendAllText(logPath, logEntry);
        }
        }

        static void LogError(string filePath, string error)
        {
            Console.WriteLine($"Error in file {filePath}: {error}");
        }
    }

    enum TransactionType
    {
        Deposit,
        Withdrawal
    }

    class Transaction
    {
        public DateTime Date { get; set; }
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
    }

    class DebitCardState
    {
        public decimal Balance { get; private set; }
        public int SuccessfulTransactions { get; private set; }
        public int UnsuccessfulTransactions { get; private set; }
         private  readonly object lockObject = new object();
    private  readonly object lockObject2 = new object();
    private  readonly object lockObject3 = new object();

    public void UpdateBalance(decimal amount)
        {
        lock (lockObject)
        {
            Balance += amount;
        }
        }

        public void AddSuccessfulTransaction(Transaction transaction)
        {
        lock (lockObject2)
        {
            SuccessfulTransactions++;
        }
        }

        public void AddUnsuccessfulTransaction(Transaction transaction)
        {
        lock (lockObject3)
        {
            UnsuccessfulTransactions++;
        }
        }

        public bool CanWithdraw(decimal amount)
        {
            return Balance >= amount;
        }
    }

    class ResultSummary
    {
        public decimal FinalBalance { get; set; }
        public int SuccessfulTransactions { get; set; }
        public int UnsuccessfulTransactions { get; set; }
        public int TotalFilesProcessed { get; set; }

        public string ToJson()
        {
            return $"{nameof(FinalBalance)}: {FinalBalance}, {nameof(SuccessfulTransactions)}: {SuccessfulTransactions}, {nameof(UnsuccessfulTransactions)}: {UnsuccessfulTransactions}, {nameof(TotalFilesProcessed)}: {TotalFilesProcessed}";
        }
    }












//Thread realisation

//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.IO;
//using System.Linq;
//using System.Threading;

//class Program
//{
//    private static readonly object lockObject = new object();

//    static void Main(string[] args)
//    {

//        string inputFolder = "/Users/tim/Projects/emulator/emulator/input";
//        string outputFolder = "/Users/tim/Projects/emulator/emulator/output";

//        var csvFiles = Directory.GetFiles(inputFolder, "*.csv")
//                                .OrderBy(file => file)
//                                .ToList();

//        DebitCardState cardState = new DebitCardState();
//        List<Thread> threads = new List<Thread>();

//        foreach (var csvFile in csvFiles)
//        {
//            Thread thread = new Thread(() => ProcessFile(csvFile, cardState, outputFolder));
//            threads.Add(thread);
//            thread.Start();
//        }

//        foreach (var thread in threads)
//        {
//            thread.Join();
//        }

//        ResultSummary resultSummary = new ResultSummary
//        {
//            FinalBalance = cardState.Balance,
//            SuccessfulTransactions = cardState.SuccessfulTransactions,
//            UnsuccessfulTransactions = cardState.UnsuccessfulTransactions,
//            TotalFilesProcessed = csvFiles.Count
//        };

//        File.WriteAllText(Path.Combine(outputFolder, "result.json"), resultSummary.ToJson());
//    }

//    static void ProcessFile(string filePath, DebitCardState cardState, string outputFolder)
//    {
//        try
//        {
//            lock (lockObject)
//            {
//                var lines = File.ReadAllLines(filePath);

//                foreach (var line in lines)
//                {
//                    var parts = line.Split(',');

//                    if (parts.Length == 3)
//                    {
//                        var dateStr = parts[0];
//                        var typeStr = parts[1];
//                        var amountStr = parts[2];

//                        if (DateTime.TryParse(dateStr, out DateTime date) &&
//                            Enum.TryParse<TransactionType>(typeStr, true, out TransactionType transactionType) &&
//                            decimal.TryParse(amountStr, NumberStyles.Currency, CultureInfo.InvariantCulture, out decimal amount))
//                        {
//                            var transaction = new Transaction
//                            {
//                                Date = date,
//                                Type = transactionType,
//                                Amount = amount
//                            };

//                            ProcessTransaction(transaction, cardState, outputFolder);
//                        }
//                        else
//                        {
//                            LogError(filePath, "Invalid format in CSV line: " + line);
//                        }
//                    }
//                    else
//                    {
//                        LogError(filePath, "Invalid CSV line: " + line);
//                    }
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            LogError(filePath, "Error reading CSV file: " + ex.Message);
//        }
//    }



//static void ProcessTransaction(Transaction transaction, DebitCardState cardState, string outputFolder)
//    {
//        lock (lockObject)
//        {
//            if (transaction.Type == TransactionType.Deposit)
//            {
//                cardState.UpdateBalance(transaction.Amount);
//                cardState.AddSuccessfulTransaction(transaction);
//                LogTransaction(outputFolder, transaction, "Success");
//            }
//            else if (transaction.Type == TransactionType.Withdrawal)
//            {
//                if (cardState.CanWithdraw(transaction.Amount))
//                {
//                    cardState.UpdateBalance(-transaction.Amount);
//                    cardState.AddSuccessfulTransaction(transaction);
//                    LogTransaction(outputFolder, transaction, "Success");
//                }
//                else
//                {
//                    cardState.AddUnsuccessfulTransaction(transaction);
//                    LogTransaction(outputFolder, transaction, "Failed (Insufficient funds)");
//                }
//            }
//        }
//    }

//    static void LogTransaction(string outputFolder, Transaction transaction, string status)
//    {
//        string logPath = Path.Combine(outputFolder, "transaction_log.csv");
//        string logEntry = $"{transaction.Date},{transaction.Type},{transaction.Amount},{status}\n";
//        File.AppendAllText(logPath, logEntry);
//    }

//    static void LogError(string filePath, string error)
//    {
//        Console.WriteLine($"Error in file {filePath}: {error}");
//    }
//}

//enum TransactionType
//{
//    Deposit,
//    Withdrawal
//}

//class Transaction
//{
//    public DateTime Date { get; set; }
//    public TransactionType Type { get; set; }
//    public decimal Amount { get; set; }
//}

//class DebitCardState
//{
//    public decimal Balance { get; private set; }
//    public int SuccessfulTransactions { get; private set; }
//    public int UnsuccessfulTransactions { get; private set; }

//    public void UpdateBalance(decimal amount)
//    {
//        Balance += amount;
//    }

//    public void AddSuccessfulTransaction(Transaction transaction)
//    {
//        SuccessfulTransactions++;
//    }

//    public void AddUnsuccessfulTransaction(Transaction transaction)
//    {
//        UnsuccessfulTransactions++;
//    }

//    public bool CanWithdraw(decimal amount)
//    {
//        return Balance >= amount;
//    }
//}

//class ResultSummary
//{
//    public decimal FinalBalance { get; set; }
//    public int SuccessfulTransactions { get; set; }
//    public int UnsuccessfulTransactions { get; set; }
//    public int TotalFilesProcessed { get; set; }

//    public string ToJson()
//    {
//        return $"{nameof(FinalBalance)}: {FinalBalance}, {nameof(SuccessfulTransactions)}: {SuccessfulTransactions}, {nameof(UnsuccessfulTransactions)}: {UnsuccessfulTransactions}, {nameof(TotalFilesProcessed)}: {TotalFilesProcessed}";
//    }
//}




//Task realisation


//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.IO;
//using System.Threading.Tasks;

//class Program
//{
//    static async Task Main(string[] args)
//    {
//        string inputFolder = "/Users/tim/Projects/emulator/emulator/input";
//        string outputFolder = "/Users/tim/Projects/emulator/emulator/output";
//        var csvFiles = Directory.GetFiles(inputFolder, "*.csv")
//                                .OrderBy(file => file)
//                                .ToList();

//        DebitCardState cardState = new DebitCardState();

//        await Task.WhenAll(csvFiles.Select(file => ProcessFileAsync(file, cardState, outputFolder)));

//        ResultSummary resultSummary = new ResultSummary
//        {
//            FinalBalance = cardState.Balance,
//            SuccessfulTransactions = cardState.SuccessfulTransactions,
//            UnsuccessfulTransactions = cardState.UnsuccessfulTransactions,
//            TotalFilesProcessed = csvFiles.Count
//        };

//        File.WriteAllText(Path.Combine(outputFolder, "result.json"), resultSummary.ToJson());
//    }

//    static async Task ProcessFileAsync(string filePath, DebitCardState cardState, string outputFolder)
//    {
//        try
//        {
//            var lines = await File.ReadAllLinesAsync(filePath);

//            foreach (var line in lines)
//            {
//                var parts = line.Split(',');

//                if (parts.Length == 3)
//                {
//                    var dateStr = parts[0];
//                    var typeStr = parts[1];
//                    var amountStr = parts[2];

//                    if (DateTime.TryParse(dateStr, out DateTime date) &&
//                        Enum.TryParse<TransactionType>(typeStr, true, out TransactionType transactionType) &&
//                        decimal.TryParse(amountStr, NumberStyles.Currency, CultureInfo.InvariantCulture, out decimal amount))
//                    {
//                        var transaction = new Transaction
//                        {
//                            Date = date,
//                            Type = transactionType,
//                            Amount = amount
//                        };

//                        ProcessTransaction(transaction, cardState, outputFolder);
//                    }
//                    else
//                    {
//                        LogError(filePath, "Invalid format in CSV line: " + line);
//                    }
//                }
//                else
//                {
//                    LogError(filePath, "Invalid CSV line: " + line);
//                }
//            }
//        }
//        catch (Exception ex)
//        {
//            LogError(filePath, "Error reading CSV file: " + ex.Message);
//        }
//    }

//    static void ProcessTransaction(Transaction transaction, DebitCardState cardState, string outputFolder)
//    {
//        if (transaction.Type == TransactionType.Deposit)
//        {
//            cardState.UpdateBalance(transaction.Amount);
//            cardState.AddSuccessfulTransaction(transaction);
//            LogTransaction(outputFolder, transaction, "Success");
//        }
//        else if (transaction.Type == TransactionType.Withdrawal)
//        {
//            if (cardState.CanWithdraw(transaction.Amount))
//            {
//                cardState.UpdateBalance(-transaction.Amount);
//                cardState.AddSuccessfulTransaction(transaction);
//                LogTransaction(outputFolder, transaction, "Success");
//            }
//            else
//            {
//                cardState.AddUnsuccessfulTransaction(transaction);
//                LogTransaction(outputFolder, transaction, "Failed (Insufficient funds)");
//            }
//        }
//    }

//    static void LogTransaction(string outputFolder, Transaction transaction, string status)
//    {
//        string logPath = Path.Combine(outputFolder, "transaction_log.csv");
//        string logEntry = $"{transaction.Date},{transaction.Type},{transaction.Amount},{status}\n";
//        File.AppendAllText(logPath, logEntry);
//    }

//    static void LogError(string filePath, string error)
//    {
//        Console.WriteLine($"Error in file {filePath}: {error}");
//    }
//}

//enum TransactionType
//{
//    Deposit,
//    Withdrawal
//}

//class Transaction
//{
//    public DateTime Date { get; set; }
//    public TransactionType Type { get; set; }
//    public decimal Amount { get; set; }
//}

//class DebitCardState
//{
//    public decimal Balance { get; private set; }
//    public int SuccessfulTransactions { get; private set; }
//    public int UnsuccessfulTransactions { get; private set; }

//    public void UpdateBalance(decimal amount)
//    {
//        Balance += amount;
//    }

//    public void AddSuccessfulTransaction(Transaction transaction)
//    {
//        SuccessfulTransactions++;
//    }

//    public void AddUnsuccessfulTransaction(Transaction transaction)
//    {
//        UnsuccessfulTransactions++;
//    }

//    public bool CanWithdraw(decimal amount)
//    {
//        return Balance >= amount;
//    }
//}

//class ResultSummary
//{
//    public decimal FinalBalance { get; set; }
//    public int SuccessfulTransactions { get; set; }
//    public int UnsuccessfulTransactions { get; set; }
//    public int TotalFilesProcessed { get; set; }

//    public string ToJson()
//    {
//        return $"{nameof(FinalBalance)}: {FinalBalance}, {nameof(SuccessfulTransactions)}: {SuccessfulTransactions}, {nameof(UnsuccessfulTransactions)}: {UnsuccessfulTransactions}, {nameof(TotalFilesProcessed)}: {TotalFilesProcessed}";
//    }
//}
