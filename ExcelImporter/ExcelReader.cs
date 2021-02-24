using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OfficeOpenXml;

namespace ExcelImporter
{
    public class ExcelReader
    {
        private readonly ILogger<ExcelReader> _logger;
        private readonly ConsoleConfig _config;

        public ExcelReader(IOptions<ConsoleConfig> configuration, ILogger<ExcelReader> logger)
        {
            _logger = logger;
            _config = configuration.Value;
        }

        public async Task RunAsync(string filePath, CancellationToken cancellationToken)
        {
            _logger.LogInformation("RunAsync");

            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                throw new Exception($"file not found in {filePath}");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage(fileInfo);
            var worksheet = package.Workbook.Worksheets[0];
            
            int emailColumnIndex = 0;
            int nameColumnIndex = 0;
            int lastNameColumnIndex = 0;

            for (int i = worksheet.Dimension.Start.Column; i < worksheet.Dimension.End.Column + 1; i++)
            {

                string columnName = worksheet.Cells[worksheet.Dimension.Start.Row, i].GetValue<string>()?.Trim().ToLower()??string.Empty;
                switch (columnName)
                {
                    case "email":
                        emailColumnIndex = i;
                        break;
                    case "e-mail":
                        emailColumnIndex = i;
                        break;
                    case "mail":
                        emailColumnIndex = i;
                        break;
                    case "nome":
                        nameColumnIndex = i;
                        break;
                    case "name":
                        nameColumnIndex = i;
                        break;
                    case "firstname":
                        nameColumnIndex = i;
                        break;
                    case "first name":
                        nameColumnIndex = i;
                        break;
                    case "cognome":
                        lastNameColumnIndex = i;
                        break;
                    case "lastname":
                        lastNameColumnIndex = i;
                        break;
                    case "last name":
                        lastNameColumnIndex = i;
                        break;
                    case "last-name":
                        lastNameColumnIndex = i;
                        break;
                    case "family name":
                        lastNameColumnIndex = i;
                        break;
                    case "familyname":
                        lastNameColumnIndex = i;
                        break;
                }

                if (emailColumnIndex == 0 || nameColumnIndex == 0 || lastNameColumnIndex == 0)
                {
                    throw new Exception("mandatory columns not found");
                }

                string email = worksheet.Cells[i, emailColumnIndex].GetValue<string>()?.Trim().ToLower() ?? string.Empty;
                string name = worksheet.Cells[i, nameColumnIndex].GetValue<string>()?.Trim() ?? string.Empty;
                string lastName = worksheet.Cells[i, lastNameColumnIndex].GetValue<string>()?.Trim() ?? string.Empty;


                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(lastName))
                {
                    throw new Exception($"mandatory value not found at line {i}");
                }

            }

        }
    }

}