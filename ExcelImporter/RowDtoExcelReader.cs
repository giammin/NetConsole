using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OfficeOpenXml;

namespace ExcelImporter
{
    public class RowDtoExcelReader : ExcelReaderBase<RowDto>
    {
        public RowDtoExcelReader(IOptions<ConsoleConfig> configuration, ILogger<RowDtoExcelReader> logger) : base(configuration, logger)
        {
        }

        protected override RowDto MapRow(ExcelWorksheet worksheet, int i, Dictionary<string, int> columnMapping)
        {
            var rowDto = new RowDto
            {
                Name = GetCellValue<string>(worksheet.Cells, i, columnMapping, nameof(RowDto.Name))?.Trim(),
                LastName = GetCellValue<string>(worksheet.Cells, i, columnMapping, nameof(RowDto.LastName))?.Trim(),
                Email = GetCellValue<string>(worksheet.Cells, i, columnMapping, nameof(RowDto.Email))?.Trim(),
                Organization = GetCellValue<string>(worksheet.Cells, i, columnMapping, nameof(RowDto.Organization))?.Trim()
            };
            return rowDto;
        }
        protected override void ValidateColumnMapping(Dictionary<string, int> columnMapping)
        {
            if (!columnMapping.ContainsKey(nameof(RowDto.Email)) || !columnMapping.ContainsKey(nameof(RowDto.Name)) ||
                !columnMapping.ContainsKey(nameof(RowDto.LastName)))
            {
                throw new Exception("mandatory columns not found");
            }
        }

        protected override void MapColumns(string columnName, int i1, Dictionary<string, int> columnMapping)
        {
            switch (columnName)
            {
                case "email" or "e-mail" or "mail":
                    AddColumnMapping(nameof(RowDto.Email), i1, columnMapping);
                    break;
                case "nome" or "name" or "firstname" or "first name":
                    AddColumnMapping(nameof(RowDto.Name), i1, columnMapping);
                    break;
                case "cognome" or "lastname" or "last name" or "last-name" or "family name" or "familyname":
                    AddColumnMapping(nameof(RowDto.LastName), i1, columnMapping);
                    break;
                case "organization":
                    AddColumnMapping(nameof(RowDto.Organization), i1, columnMapping);
                    break;
            }
        }
    }
}