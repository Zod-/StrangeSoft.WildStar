using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace StrangeSoft.WildStar.Database
{
    public static class WildstarDatabaseExtensions
    {
        public static WildstarDatabase ToDatabase(this IArchiveDirectoryEntry directory)
        {
            return new WildstarDatabase(directory);
        }

        public static WildstarDatabase ToDatabase(this WildstarAssets assets)
        {
            return new WildstarDatabase(assets);
        }

        public static WildstarTable ToTable(this IArchiveFileEntry fileEntry)
        {
            return new WildstarTable(fileEntry.Open());
        }

        public static WildstarTable ToTable(this FileInfo fileInfo)
        {
            return new WildstarTable(fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
        }

        public static void DumpToCsv(this WildstarTable tableObject, string fileName)
        {
            using (
    var streamWriter =
        new StreamWriter(File.Open(fileName,   //$@"D:\WSData\Live\DB\{tableObject.TableHeader.TableName}.csv",
            FileMode.Create, FileAccess.Write, FileShare.ReadWrite)))
            {
                streamWriter.WriteLine(string.Join(",", tableObject.TableFieldDescriptors));
                foreach (var row in tableObject.Rows)
                {
                    streamWriter.WriteLine(string.Join(",", row.Columns));
                }
            }
        }

        public static void DumpToCsv(this FileInfo file, string fileName)
        {
            using (var table = file.ToTable())
            {
                table.DumpToCsv(fileName);
            }
        }

        public static void DumpToCsv(this IArchiveFileEntry file, string fileName)
        {
            using (var table = file.ToTable())
            {
                table.DumpToCsv(fileName);
            }
        }

        public static void DumpToSql(this WildstarTable table, string fileName)
        {
            using (var streamWriter = new StreamWriter(File.Open(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite)))
                table.DumpToSql(streamWriter);
        }

        public static void DumpToSql(this WildstarTable table, StreamWriter streamWriter)
        {
            streamWriter.WriteLine();
            streamWriter.WriteLine($"--- Table: {table.TableHeader.TableName}");
            streamWriter.WriteLine($"--- Version: {table.TableHeader.Version}");

            streamWriter.WriteLine($"IF NOT EXISTS (SELECT * FROM sysobjects where name='{table.TableHeader.TableName}' and xtype='U')");
            streamWriter.WriteLine("BEGIN");
            streamWriter.WriteLine($"\tCREATE TABLE [dbo].[{table.TableHeader.TableName}] (");
            List<Func<object, string>> fieldWriters = new List<Func<object, string>>();
            foreach (var field in table.TableFieldDescriptors)
            {
                if (fieldWriters.Count > 0)
                {
                    streamWriter.WriteLine(",");
                }
                string sqlDataTypeName = null;
                switch (field.FieldType)
                {
                    case FieldType.UInt32:

                        sqlDataTypeName = "NUMERIC(10) NOT NULL";
                        fieldWriters.Add(o => o.ToString());
                        break;
                    case FieldType.UInt64:
                        sqlDataTypeName = "NUMERIC(20) NOT NULL";
                        fieldWriters.Add(o => o.ToString());
                        break;
                    case FieldType.Float:
                        sqlDataTypeName = "FLOAT NOT NULL";
                        fieldWriters.Add(o => o.ToString());
                        break;
                    case FieldType.Bool:
                        sqlDataTypeName = "BIT NOT NULL";
                        fieldWriters.Add(o => (bool)o ? "CAST(1 AS BIT)" : "CAST(0 AS BIT)");
                        break;
                    case FieldType.StringTableOffset:
                        sqlDataTypeName = "NVARCHAR(400)";
                        fieldWriters.Add(o => o == null ? "NULL" : $"'{o.ToString().Replace("'", "''")}'");
                        break;
                }

                streamWriter.Write("\t\t");
                streamWriter.Write($"[{field.Title}] ");
                streamWriter.Write(sqlDataTypeName);
            }
            streamWriter.WriteLine();
            streamWriter.WriteLine("\t\t);");
            streamWriter.WriteLine("\tALTER TABLE [dbo].[{0}] ADD CONSTRAINT PK_{0}_{1} PRIMARY KEY CLUSTERED ({1});", table.TableHeader.TableName, table.TableFieldDescriptors.First().Title);
            streamWriter.WriteLine("END");
            streamWriter.WriteLine("GO");
            streamWriter.WriteLine();
            streamWriter.WriteLine($"TRUNCATE TABLE [dbo].[{table.TableHeader.TableName}];");
            streamWriter.WriteLine("GO");
            streamWriter.WriteLine();
            bool first = true;
            var insertStatementStart =
                $"INSERT INTO [dbo].[{table.TableHeader.TableName}] ({string.Join(", ", table.TableFieldDescriptors.Select(i => $"[{i.Title}]"))}) VALUES";
            int counter = 0;
            foreach (var row in table.Rows)
            {
                counter = (counter % 100) + 1;
                if (counter == 1)
                {
                    streamWriter.WriteLine();
                    if (!first)
                    {
                        streamWriter.WriteLine("GO");
                    }
                    else
                    {
                        first = false;
                    }
                    streamWriter.WriteLine(insertStatementStart);
                }
                else
                {
                    streamWriter.WriteLine(",");
                }
                streamWriter.Write("\t(");
                for (var x = 0; x < fieldWriters.Count; x++)
                {
                    if (x != 0)
                    {
                        streamWriter.Write(", ");
                    }
                    streamWriter.Write(fieldWriters[x](row.Columns[x].Value));
                }
                streamWriter.Write(")");
            }

            streamWriter.WriteLine();
            streamWriter.WriteLine("GO");
            streamWriter.WriteLine("--- END OF TABLE {0}", table.TableHeader.TableName);
            streamWriter.WriteLine();
        }

        public static void DumpToSql(this WildstarDatabase database, string fileName)
        {
            using (var streamWriter = new StreamWriter(File.Open(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite)))
                foreach (var table in database)
                {
                    table.DumpToSql(streamWriter);
                }
        }
    }
}