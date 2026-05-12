using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharedContracts.Import;

namespace PaletixDesktopWinUI.Tests
{
    [TestClass]
    public sealed class BulkImportParserTests
    {
        [TestMethod]
        public void CsvParsesSemicolonHeadersAndRequiredFields()
        {
            var result = BulkImportParser.Parse(
                "Referencia;Nom;Quantitat\nSKU-1;Beguda;12",
                BulkImportFormat.Csv,
                () => new ImportTestItem(),
                Columns());

            Assert.AreEqual(1, result.ValidCount);
            Assert.AreEqual("SKU-1", result.Rows[0].Item.Reference);
            Assert.AreEqual("Beguda", result.Rows[0].Item.Name);
            Assert.AreEqual(12, result.Rows[0].Item.Quantity);
        }

        [TestMethod]
        public void JsonParsesAliasesCaseInsensitive()
        {
            var result = BulkImportParser.Parse(
                "[{\"ref\":\"SKU-2\",\"name\":\"Caixa\",\"quantitat\":4}]",
                BulkImportFormat.Json,
                () => new ImportTestItem(),
                Columns());

            Assert.AreEqual(1, result.ValidCount);
            Assert.AreEqual("SKU-2", result.Rows[0].Item.Reference);
            Assert.AreEqual("Caixa", result.Rows[0].Item.Name);
            Assert.AreEqual(4, result.Rows[0].Item.Quantity);
        }

        [TestMethod]
        public void MissingRequiredFieldReturnsRowError()
        {
            var result = BulkImportParser.Parse(
                "Referencia;Quantitat\nSKU-3;2",
                BulkImportFormat.Csv,
                () => new ImportTestItem(),
                Columns());

            Assert.AreEqual(0, result.ValidCount);
            Assert.AreEqual(1, result.ErrorCount);
            StringAssert.Contains(result.Rows[0].ErrorText(), "Nom");
        }

        private static BulkImportColumn<ImportTestItem>[] Columns()
        {
            return new[]
            {
                new BulkImportColumn<ImportTestItem>("Referencia", "Referencia", false, (item, value) => item.Reference = value, "ref"),
                new BulkImportColumn<ImportTestItem>("Nom", "Nom", true, (item, value) => item.Name = value, "name"),
                new BulkImportColumn<ImportTestItem>("Quantitat", "Quantitat", true, (item, value) => item.Quantity = int.Parse(value), "quantity")
            };
        }

        private sealed class ImportTestItem
        {
            public string Reference { get; set; } = "";
            public string Name { get; set; } = "";
            public int Quantity { get; set; }
        }
    }

    internal static class BulkImportRowTestExtensions
    {
        public static string ErrorText<T>(this BulkImportRow<T> row)
        {
            return string.Join(" ", row.Errors);
        }
    }
}
