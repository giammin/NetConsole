using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OfficeOpenXml;

namespace ExcelImporter
{
    public abstract class ExcelReaderBase<T>
    {
        protected readonly ILogger<ExcelReaderBase<T>> Logger;
        protected readonly ConsoleConfig Config;

        protected ExcelReaderBase(IOptions<ConsoleConfig> configuration, ILogger<ExcelReaderBase<T>> logger)
        {
            Logger = logger;
            Config = configuration.Value;
        }

        public async Task RunAsync(string filePath, CancellationToken cancellationToken)
        {
            Logger.LogInformation("RunAsync");

            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
            {
                throw new Exception($"file not found in {filePath}");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage(fileInfo);
            var worksheet = package.Workbook.Worksheets[0];

            var columnMapping = new Dictionary<string, int>();

            for (int i = worksheet.Dimension.Start.Column; i < worksheet.Dimension.End.Column + 1; i++)
            {
                string columnName = worksheet.Cells[worksheet.Dimension.Start.Row, i].GetValue<string>()?.Trim().ToLower()??string.Empty;

                MapColumns(columnName, i, columnMapping);
            }
            ValidateColumnMapping(columnMapping);

            for (int i = worksheet.Dimension.Start.Row + 1; i <= worksheet.Dimension.End.Row; i++)
            {
                var rowDto = MapRow(worksheet, i, columnMapping);

                var ctx = new ValidationContext(rowDto);


                var results = new List<ValidationResult>();
                if (!Validator.TryValidateObject(rowDto, ctx, results, true))
                {
                    Logger.LogError($"{i} - ERROR FOUND {results.Count}");
                    foreach (var error in results)
                    {
                        Logger.LogError($"\t{error}");
                    }
                }
                else
                {
                    Logger.LogInformation($"{i} - {rowDto}");
                }
            }

        }
        protected static void AddColumnMapping(string propertyName, int i, Dictionary<string, int> columnMapping)
        {
            if (columnMapping.ContainsKey(propertyName))
            {
                throw new Exception($"duplicated column name at columns {columnMapping[propertyName]} {i}");
            }

            columnMapping.Add(propertyName, i);
        }

        protected abstract T MapRow(ExcelWorksheet worksheet, int i, Dictionary<string, int> columnMapping);
        protected abstract void ValidateColumnMapping(Dictionary<string, int> columnMapping);
        protected abstract void MapColumns(string columnName, int i1, Dictionary<string, int> columnMapping);

        protected static TC? GetCellValue<TC>(ExcelRange cells, int row, Dictionary<string, int> columnMapping, string columnName)
        {
            var rtn = default(TC);
            if (columnMapping.ContainsKey(columnName))
            {
                rtn = cells[row, columnMapping[columnName]].GetValue<TC>();
            }
            return rtn;
        }

    }
}