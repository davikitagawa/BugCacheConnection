using InterSystems.Data.CacheClient;
using System.Data;
using System.Data.Common;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BugCacheConnection
{
    public class BugCacheConnection
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("------------------ISSUE EXAMPLE------------------");

            string? connectionString = GetArg(args, "connectionString");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("Invalid connection string. Please check the arguments.");
            }

            // Open the connection using the connectionString argument
            await DatabaseConnection.OpenConnection(connectionString);

            // Get the path argument if exists. Should be defined as "--id=yourImageID"
            string? idImageByteArray = GetArg(args, "id");

            // Get the path argument if exists. Should be defined as "--imagePath=PathToYourImage/yourImg.jpeg"
            string? path = GetArg(args, "imagePath");

            // Insert some rows in table with an image sent by path to make it easier to reproduce the problem
            if (!string.IsNullOrEmpty(path))
            {
                // Get the image byte array from informed path or actual working directory
                byte[] imageByteArray = GetImageByteArray(path);

                // IMPORTANT: The error only occurs when the table has many rows (in our local test, at least 19 rows were enough to reproduce the problem)
                for (int i = 0; i <= 18; i++)
                {
                    // Insert the image on database
                    await InsertImageByteArray(imageByteArray);
                }
            }

            // Define the query base for both execution
            string sqlBase = "SELECT Name, Version, Type, LastUpdateTimestamp, LastUpdateUser, ByteArrayColumn FROM MCI.Student ";

            // Define the query that works correctly
            string sqlFiltered = sqlBase + "WHERE ID = @id";

            try
            {
                await Execute(sqlFiltered, idImageByteArray);
            }
            catch (Exception exc)
            {
                // It shall not reach here
                Console.WriteLine("Shows this if an exception was thrown by FILTERED QUERY");
            }

            // Define the query that does not work
            string sqlNotFiltered = sqlBase;

            try
            {
                await Execute(sqlNotFiltered);
            }
            catch (Exception exc)
            {
                // It will always reach here
                Console.WriteLine("Shows this if an exception was thrown by **NOT** FILTERED QUERY");
            }

            // Closes the connection
            await DatabaseConnection.CONNECTION!.CloseAsync();

            Console.WriteLine("------------------Execution End------------------");
        }

        private static async Task Execute(string sql, string? id = null)
        {
            string text = id != null ? $"for id {id}" : "for all rows";

            Console.WriteLine();
            Console.WriteLine($"Executing query {text}");

            // Creates a command
            using CacheCommand command = DatabaseConnection.GetCommand(sql);

            // Use parameter filter only if defined
            if (!string.IsNullOrEmpty(id) && int.TryParse(id, out int intId))
            {
                command.Parameters.Add("id", id);
            }

            // Execute the command
            using IDataReader reader = await command.ExecuteReaderAsync();

            // Fething data
            while (reader.Read())
            {
                Dictionary<string, object> linha = new();

                for (int index = 0; index < reader.FieldCount; index++)
                {
                    string columnName = reader.GetName(index);
                    object value = reader.GetValue(index);

                    linha[columnName] = value;

                    Console.Write($"{columnName}: {value}    ");
                }

                Console.WriteLine();
            }
        }

        private static string? GetArg(string[] args, string argument)
        {
            string? sValor = args.Where(arg => arg.StartsWith($"--{argument}=")).FirstOrDefault();

            if (string.IsNullOrEmpty(sValor))
            {
                return null;
            }
            else
            {
                return sValor.Substring($"--{argument}=".Length);
            }
        }

        private static byte[] GetImageByteArray(string path)
        {
            if (!File.Exists(path))
            {
                throw new Exception("Image not found. Check the path argument");
            }

            if (!OperatingSystem.IsWindows())
            {
                throw new Exception("Incorrect Operating System. This repo should be running only in Windows");
            }

            Image img = Image.FromFile(path);

            return ImageToByteArray(img);
        }

        private static byte[] ImageToByteArray(Image imageIn)
        {
            using (var ms = new MemoryStream())
            {
                if (!OperatingSystem.IsWindows())
                {
                    throw new Exception("Incorrect Operating System. This repo should be running only in Windows");
                }

                imageIn.Save(ms, imageIn.RawFormat);
                return ms.ToArray();
            }
        }

        private async static Task InsertImageByteArray(byte[] imageByteArray)
        {
            string sql = @"
INSERT INTO MCI.Student
(Name, Version, Type, LastUpdateTimestamp, LastUpdateUser, ByteArrayColumn, Active)
VALUES
(@Name, @Version, @Type, @LastUpdateTimestamp, @LastUpdateUser, @ByteArrayColumn, @Active)";

            // Creates a command
            using CacheCommand command = DatabaseConnection.GetCommand(sql);

            // Adds the image byte array parameter
            command.Parameters.AddWithValue("Name", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("Version", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("Type", 1);
            command.Parameters.AddWithValue("LastUpdateTimestamp", DateTime.Now);
            command.Parameters.AddWithValue("LastUpdateUser", Guid.NewGuid().ToString());
            command.Parameters.AddWithValue("ByteArrayColumn", imageByteArray);
            command.Parameters.AddWithValue("Active", true);

            // Execute the command
            await command.ExecuteScalarAsync();
        }
    }
}

public class DatabaseConnection
{
    public static DbConnection? CONNECTION;

    public static async Task OpenConnection(string stringConnection)
    {
        CONNECTION = new CacheConnection(stringConnection);

        await CONNECTION.OpenAsync();
    }

    public static CacheCommand GetCommand(string commandText)
    {
        // Cria um command através da conexão e transação
        return new CacheCommand(commandText, (CacheConnection)CONNECTION!);
    }
}
