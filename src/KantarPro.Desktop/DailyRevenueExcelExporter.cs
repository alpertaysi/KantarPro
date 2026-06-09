using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Text;

namespace KantarPro.Desktop
{
    public static class DailyRevenueExcelExporter
    {
        private static readonly string[] Headers =
        {
            "Sıra",
            "İşlem No",
            "İşlem Tipi",
            "Kantar Fiş No",
            "Ödeme Türü",
            "Firma",
            "Plaka",
            "Çıkış Tarihi",
            "Çıkış Saati",
            "Giriş Ücreti",
            "Tartım Ücreti",
            "İşgaliye Ücreti",
            "Toplam Ücret"
        };

        public static void Export(string path, IEnumerable<DailyRevenueRow> rows)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Excel dosya yolu boş olamaz.", nameof(path));
            }

            var list = (rows ?? Enumerable.Empty<DailyRevenueRow>()).ToList();
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                WriteEntry(archive, "[Content_Types].xml", BuildContentTypes());
                WriteEntry(archive, "_rels/.rels", BuildRootRelationships());
                WriteEntry(archive, "xl/workbook.xml", BuildWorkbook());
                WriteEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelationships());
                WriteEntry(archive, "xl/worksheets/sheet1.xml", BuildWorksheet(list));
            }
        }

        private static string BuildWorksheet(IList<DailyRevenueRow> rows)
        {
            var builder = new StringBuilder();
            builder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            builder.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
            AppendRow(builder, 1, Headers);

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                AppendRow(builder, i + 2, new[]
                {
                    row.SiraNo.ToString(),
                    row.IslemNo,
                    row.IslemTipi,
                    row.KantarFisNo,
                    row.OdemeTuru,
                    row.FirmaAdi,
                    row.Plaka,
                    row.CikisTarihi,
                    row.CikisSaati,
                    row.GirisCikisUcreti,
                    row.TartimUcreti,
                    row.BeklemeUcreti,
                    row.ToplamUcret
                });
            }

            builder.Append("</sheetData></worksheet>");
            return builder.ToString();
        }

        private static void AppendRow(StringBuilder builder, int rowNumber, IEnumerable<string> values)
        {
            builder.Append("<row r=\"").Append(rowNumber).Append("\">");
            foreach (var value in values)
            {
                builder.Append("<c t=\"inlineStr\"><is><t xml:space=\"preserve\">")
                    .Append(Escape(value))
                    .Append("</t></is></c>");
            }

            builder.Append("</row>");
        }

        private static string Escape(string value)
        {
            return SecurityElement.Escape(value ?? string.Empty) ?? string.Empty;
        }

        private static void WriteEntry(ZipArchive archive, string path, string content)
        {
            var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
            using (var stream = entry.Open())
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(content);
            }
        }

        private static string BuildContentTypes()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>" +
                "<Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
                "<Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>" +
                "</Types>";
        }

        private static string BuildRootRelationships()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                "</Relationships>";
        }

        private static string BuildWorkbook()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" " +
                "xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
                "<sheets><sheet name=\"Günlük Tahsilat\" sheetId=\"1\" r:id=\"rId1\"/></sheets>" +
                "</workbook>";
        }

        private static string BuildWorkbookRelationships()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/>" +
                "</Relationships>";
        }
    }
}
