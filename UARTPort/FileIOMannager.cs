using CsvHelper;
using Microsoft.VisualBasic.FileIO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static UARTPort.CsvIO;

namespace UARTPort
{
    internal class FileIOFactor
    {
        public FileIOMannager CreateFileIO(string fileType)
        {
            if (fileType == "txt")
            {
                return new TxtIO(fileType);
            }
            if (fileType == "csv")
            {
                return new CsvIO(fileType);
            }
            if (fileType == "excel")
            {
                return new ExcelIO(fileType);
            }
            throw new NotImplementedException();
        }
    }
    public class SerialData
    {
        public int? Count { get; set; }
        public string? PortNumber { get; set; }
        public string? Date { get; set; }
        public string? State { get; set; }
        public string? Data { get; set; }
    }
    internal abstract class FileIOMannager
    {
        public SerialData serialData;
        public int DataCount;
        public string FileIOType { get; set; }
        public string FileAddress { get; set; }
        public abstract void InitFileAddressAndStreamClass();
        public abstract void Write(string value);
        public abstract string Read(string path);
        public abstract void Close();
        public SerialData SplitSerialString(string value)
        {
            serialData = new SerialData();
            string[] strings = value.Split("\r\n");
            string[] buffer = new string[5];
            buffer[0] = DataCount.ToString();
            DataCount++;
            for (int i = 0; i < 4; i++)
            {
                string[] temp = strings[i].Split("=");
                if (temp.Length == 2)
                {
                    buffer[i + 1] = temp[1];
                }
                else
                {
                    buffer[i + 1] = temp[0];
                }
            }
            serialData.Count = Convert.ToInt16(buffer[0]);
            serialData.PortNumber = buffer[1];
            serialData.Date = buffer[2];
            serialData.State = buffer[3];
            serialData.Data = buffer[4];
            return serialData;
        }
    }
    internal class TxtIO : FileIOMannager
    {
        private FileStream fileStream;
        private string ReadContent;

        public TxtIO(string fileType)
        {
            DataCount = 0;
            FileIOType = fileType;
        }
        public override void InitFileAddressAndStreamClass()
        {
            string currentDirectory = AppContext.BaseDirectory;
            FileAddress = currentDirectory + DateTime.Now.ToString().Replace(" ", "").Replace("/", ".").Replace(":", ".") + "." + FileIOType;
            fileStream = new FileStream(FileAddress, FileMode.Append);
        }
        public override void Write(string value)
        {
            value = "Count=" + DataCount.ToString() + "\r\n" + value;
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            fileStream.WriteAsync(bytes);
        }
        public override string Read(string path)
        {
            if (path.Substring(path.Length - 3, 3).Contains("txt"))
            {
                return ReadContent = File.ReadAllTextAsync(path).Result;
            }
            else
            {
                throw new Exception("文件类型错误");
            }
        }
        public override void Close()
        {
            if (fileStream!=null)
            {
                fileStream.Close();
            }
        }

    }
    internal class CsvIO : FileIOMannager 
    {
        
        public CsvIO(string fileType)
        {
            DataCount = 0;
            FileIOType = fileType;
        }
        public override void InitFileAddressAndStreamClass()
        {
            string currentDirectory = AppContext.BaseDirectory;
            FileAddress = currentDirectory + DateTime.Now.ToString().Replace(" ", "").Replace("/", ".").Replace(":", ".") + "." + FileIOType;
            using (StreamWriter fileStream = new StreamWriter(FileAddress, append: true))
            using (CsvWriter csvWriter = new CsvWriter(fileStream, CultureInfo.InvariantCulture))
            {
                csvWriter.WriteField("Count");
                csvWriter.WriteField("PortNumber");
                csvWriter.WriteField("Date");
                csvWriter.WriteField("State");
                csvWriter.WriteField("Data");
                csvWriter.NextRecord();
            }
            
        }
        public override void Write(string value)
        {
            SerialData serialData = SplitSerialString(value);
            using (StreamWriter fileStream = new StreamWriter(FileAddress, append:true))
            using (CsvWriter csvWriter = new CsvWriter(fileStream, CultureInfo.InvariantCulture))
            {
                csvWriter.WriteRecord(serialData);
                csvWriter.NextRecord(); 
            }
        }
        
        public override string Read(string path)
        {
            string result = string.Empty;
            using (StreamReader fileReader = new StreamReader(path)) 
            {
                using (CsvReader csvReader = new CsvReader(fileReader, CultureInfo.InvariantCulture))
                {
                    var records = csvReader.GetRecords<SerialData>();

                    foreach (var record in records)
                    {
                        result += $"Count: {record.Count}, PortNumber: {record.PortNumber}, Date: {record.Date}, State: {record.State}, Data: {record.Data}\n";
                    }
                }
                return result;
            }
        }

        public override void Close()
        {
        }
    }
    internal class ExcelIO : FileIOMannager
    {
        private string ConnectionString = string.Empty;
        public ExcelIO(string fileType) 
        {
            DataCount = 0;
            FileIOType = fileType;
        }
        public override void Close()
        {
           
        }

        public override void InitFileAddressAndStreamClass()
        {
            IWorkbook workbook = new XSSFWorkbook();

            ISheet sheet = workbook.CreateSheet("Sheet1");

            //新行
            IRow row = sheet.CreateRow(0);
            row.CreateCell(0).SetCellValue("Count");
            row.CreateCell(1).SetCellValue("PortNumber");
            row.CreateCell(2).SetCellValue("Date");
            row.CreateCell(3).SetCellValue("State");
            row.CreateCell(4).SetCellValue("Data");

            //保存
            FileAddress = AppContext.BaseDirectory + DateTime.Now.ToString().Replace(" ", "").Replace("/", ".").Replace(":", ".") + ".xlsx";
            using (var fs = new FileStream(FileAddress, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fs);
            }
        }

        public override string Read(string path)
        {
            string result = string.Empty;
            // 打开文件流
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                IWorkbook workbook = new XSSFWorkbook(fileStream);
                ISheet sheet = workbook.GetSheetAt(0);

                for (int rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    IRow row = sheet.GetRow(rowIndex);
                    if (row != null) 
                    {
                        for (int colIndex = 0; colIndex < row.LastCellNum; colIndex++)
                        {
                            ICell cell = row.GetCell(colIndex);
                            if (cell != null) 
                            {
                                // 根据单元格类型读取内容
                                switch (cell.CellType)
                                {
                                    case CellType.String:
                                        result += cell.StringCellValue+ "\t";
                                        break;
                                    case CellType.Numeric:
                                        result += cell.NumericCellValue.ToString() + "\t";
                                        break;
                                    default:
                                        Console.Write("\t");
                                        break;
                                }
                            }
                        }
                        result += "\r\n";
                    }
                }
            }
            return result;
        }

        public override void Write(string value)
        {
            IWorkbook workbook;
            ISheet sheet;
            using (var fileStream = new FileStream(FileAddress, FileMode.Open, FileAccess.Read))
            {
                workbook = new XSSFWorkbook(fileStream);
                sheet = workbook.GetSheet("Sheet1");
            }

            SerialData serialData = SplitSerialString(value);

            int rowCount = sheet.LastRowNum + 1;

            IRow newRow = sheet.CreateRow(rowCount);
            newRow.CreateCell(0).SetCellValue(serialData.Count.ToString());
            newRow.CreateCell(1).SetCellValue(serialData.PortNumber);
            newRow.CreateCell(2).SetCellValue(serialData.Date);
            newRow.CreateCell(3).SetCellValue(serialData.State);
            newRow.CreateCell(4).SetCellValue(serialData.Data);

            // 保存
            using (var fileStream = new FileStream(FileAddress, FileMode.Open, FileAccess.ReadWrite))
            {
                fileStream.Seek(0, SeekOrigin.Begin);
                workbook.Write(fileStream);
            }
        }
    }
}
