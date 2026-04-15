using System;
using System.IO;
using System.Globalization;
using KGV.Core.Models;
using KGV.Core.Utilities;
using PdfSharpCore.Pdf.IO;

class Program
{
    static void DumpTemplateAndMapping()
    {
        try
        {
            // Walk up from current directory to find repository root containing KGV.Core
            var dir = new DirectoryInfo(Environment.CurrentDirectory);
            DirectoryInfo? repoRoot = null;
            for (var i = 0; i < 6 && dir != null; i++)
            {
                var candidate = Path.Combine(dir.FullName, "KGV.Core", "Templates", "Mitgliedsantrag_Vorlage_Formularfelder.pdf");
                if (File.Exists(candidate)) { repoRoot = dir; break; }
                dir = dir.Parent;
            }

            var outPath = Path.Combine(Environment.CurrentDirectory, "template_mapping_diag.txt");
            var lines = new System.Collections.Generic.List<string>();
            if (repoRoot == null)
            {
                lines.Add("Could not locate repo root with KGV.Core/Templates");
                File.WriteAllLines(outPath, lines);
                return;
            }

            var templatePath = Path.Combine(repoRoot.FullName, "KGV.Core", "Templates", "Mitgliedsantrag_Vorlage_Formularfelder.pdf");
            lines.Add("Template: " + templatePath);
            using var doc = PdfReader.Open(templatePath, PdfDocumentOpenMode.ReadOnly);
            var form = doc.AcroForm;
            if (form != null)
            {
                lines.Add("Fields in template:");
                foreach (var f in form.Fields)
                {
                    try
                    {
                        var el = f.GetType().GetProperty("Elements")?.GetValue(f);
                        var getString = el?.GetType().GetMethod("GetString", new[] { typeof(string) });
                        var t = getString?.Invoke(el, new object[] { "/T" })?.ToString() ?? "(unknown)";
                        lines.Add(t);
                    }
                    catch { }
                }
            }
            else
            {
                lines.Add("No AcroForm in template.");
            }

            // Also extract raw field names by scanning PDF bytes for /T(...)
            try
            {
                var raw = File.ReadAllBytes(templatePath);
                var text = System.Text.Encoding.ASCII.GetString(raw);
                var names = new System.Collections.Generic.List<string>();
                var rx = new System.Text.RegularExpressions.Regex(@"/T\s*\(([^)]*)\)");
                foreach (System.Text.RegularExpressions.Match m in rx.Matches(text))
                {
                    var n = m.Groups[1].Value;
                    if (!string.IsNullOrWhiteSpace(n) && !names.Contains(n)) names.Add(n);
                }
                lines.Add("");
                lines.Add("Raw extracted field names:");
                if (names.Count == 0) lines.Add("(none)");
                foreach (var n in names) lines.Add(n);

                // write raw names file for easier inspection
                File.WriteAllLines(Path.Combine(Environment.CurrentDirectory, "template_fieldnames_raw.txt"), names);
            }
            catch (Exception ex)
            {
                lines.Add("Raw extraction failed: " + ex.Message);
            }

            File.WriteAllLines(outPath, lines);
        }
        catch (Exception ex) { File.WriteAllText(Path.Combine(Environment.CurrentDirectory, "template_mapping_diag_error.txt"), ex.ToString()); }
    }

    static int Main(string[] args)
    {
        // Diagnostic: dump template field names and mapping keys, compare
        DumpTemplateAndMapping();

        // Test 1: minderjährig mit gesetzlicher Vertretung
        var memberMinor = new MitgliedRecord
        {
            Id = 1,
            Vorname = "Max",
            Name = "Mustermann",
            Geburtsdatum = new DateTime(2008, 6, 1), // minderjährig in 2026
            Adresse = "Musterstraße 1",
            Plz = "12345",
            Ort = "Musterstadt",
            Telefon = "0123456789",
            Handy = "01701234567",
            Email = "max@example.org",
            WhatsappEinwilligung = true,
            EmailRechnungEinwilligung = true,
            EmailInfoEinwilligung = true,
            MitgliedSeit = new DateTime(2026, 5, 1)
        };

        var vertreter = new MitgliedsantragVertreterSnapshot
        {
            Vorname = "Erika",
            Nachname = "Mustermann",
            Adresse = "Vertreterweg 2",
            Plz = "12345",
            Ort = "Musterstadt",
            Telefon = "0987654321",
            Handy = "01707654321",
            Email = "erika@example.org"
        };

        var bank = new MitgliedsantragBankverbindungSnapshot
        {
            VereinName = "KGV Oberrothenbach",
            VereinRegisterangabe = "Amtsgericht Musterstadt VR 12345",
            VereinEmail = "kontakt@kgv.de",
            Kontoinhaber = "KGV Oberrothenbach",
            Bankname = "Musterbank",
            Iban = "DE00123456781234567890",
            Bic = "MUSTERBIC",
            DatenschutzText = "Datenschutzhinweis Muster",
            DatenschutzVersion = "1.0",
            DatenschutzStand = new DateTime(2026,1,1),
            StandardHinweistext = "Hinweis Muster",
            VerwendungszweckMitgliedsantrag = "Mitgliedsantrag"
        };

        var beginn = new DateTime(2026, 5, 1);
        var jahresbeitrag = 90.00m;
        var aufnahmegebuehr = 10.00m;

        Console.WriteLine("Creating dokument request (minor)...");
        var minorPath = Path.Combine(Environment.CurrentDirectory, "Mitgliedsantrag_Test_Minor.pdf");
        var adultPath = Path.Combine(Environment.CurrentDirectory, "Mitgliedsantrag_Test_Adult.pdf");
        try
        {
            var request = MitgliedsantragDokumentFactory.CreateUploadRequest(memberMinor, jahresbeitrag, aufnahmegebuehr, beginn, vertreter, bank, FormularDokumentStatus.Unsigniert.ToString());
            File.WriteAllBytes(minorPath, request.FileContent);
            Console.WriteLine($"PDF written to {minorPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR (minor): " + ex);
            return 2;
        }

    static void CheckPdfContains(string path, string[] needles)
    {
        Console.WriteLine($"Checking text snippets in: {path}");
        try
        {
            var bytes = File.ReadAllBytes(path);
            var text = System.Text.Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, 200000));
            foreach (var n in needles)
            {
                Console.WriteLine($"Contains '{n}': {text.Contains(n)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Text check error: " + ex.Message);
        }
    }


        // Test 2: volljährig ohne gesetzliche Vertretung
        var memberAdult = new MitgliedRecord
        {
            Id = 2,
            Vorname = "Anna",
            Name = "Beispiel",
            Geburtsdatum = new DateTime(1990, 4, 12), // volljährig
            Adresse = "Beispielweg 3",
            Plz = "54321",
            Ort = "Beispielstadt",
            Telefon = "030123456",
            Handy = "01709876543",
            Email = "anna@example.org",
            WhatsappEinwilligung = false,
            EmailRechnungEinwilligung = false,
            EmailInfoEinwilligung = false,
            MitgliedSeit = new DateTime(2026, 5, 1)
        };

        Console.WriteLine("Creating dokument request (adult)...");
        try
        {
            var request2 = MitgliedsantragDokumentFactory.CreateUploadRequest(memberAdult, jahresbeitrag, aufnahmegebuehr, beginn, null, bank, FormularDokumentStatus.Unsigniert.ToString());
            File.WriteAllBytes(adultPath, request2.FileContent);
            Console.WriteLine($"PDF written to {adultPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("ERROR (adult): " + ex);
            return 3;
        }

        Console.WriteLine("Both test PDFs generated. Inspecting fields...");
        InspectPdfFields(minorPath);
        InspectPdfFields(adultPath);

        // Additionally verify that key visible strings exist in the flattened PDF content
        Console.WriteLine("Verifying visible text in generated PDFs...");
        CheckPdfContains(minorPath, new[] { "Mustermann", "Erika", "60,00", "90,00" });
        CheckPdfContains(adultPath, new[] { "Beispiel", "Anna", "90,00" });

        Console.WriteLine("Done.");
        return 0;
    }

    static void InspectPdfFields(string path)
    {
        Console.WriteLine($"\nInspecting PDF fields for: {path}");
        if (!File.Exists(path))
        {
            Console.WriteLine("File not found.");
            return;
        }

        using var document = PdfReader.Open(path, PdfDocumentOpenMode.ReadOnly);
        var form = document.AcroForm;
        if (form == null)
        {
            Console.WriteLine("No AcroForm present.");
            return;
        }

        try
        {
            var needAppearances = "?";
            try
            {
                var ea = form.Elements;
                var getBool = ea?.GetType().GetMethod("GetBoolean", new[] { typeof(string) });
                var na = getBool?.Invoke(ea, new object[] { "/NeedAppearances" });
                needAppearances = na == null ? "(unknown)" : na.ToString();
            }
            catch { }
            Console.WriteLine($"NeedAppearances: {needAppearances}");
        }
        catch { }

        int i = 0;
        foreach (var fieldObj in form.Fields)
        {
            string fieldName = "(unknown)";
            try
            {
                var nameProp = fieldObj.GetType().GetProperty("Name");
                if (nameProp != null)
                    fieldName = nameProp.GetValue(fieldObj)?.ToString() ?? fieldName;
            }
            catch { }

            if (string.IsNullOrWhiteSpace(fieldName))
            {
                try
                {
                    var elements = fieldObj.GetType().GetProperty("Elements")?.GetValue(fieldObj);
                    var getString = elements?.GetType().GetMethod("GetString", new[] { typeof(string) });
                    var t = getString?.Invoke(elements, new object[] { "/T" })?.ToString();
                    if (!string.IsNullOrWhiteSpace(t)) fieldName = t!;
                }
                catch { }
            }

            string fieldValue = "(empty)";
            try
            {
                var valueProp = fieldObj.GetType().GetProperty("Value");
                if (valueProp != null)
                {
                    var v = valueProp.GetValue(fieldObj);
                    if (v != null) fieldValue = v.ToString() ?? fieldValue;
                }
            }
            catch { }

            if (string.IsNullOrWhiteSpace(fieldValue) || fieldValue == "(empty)")
            {
                try
                {
                    var elements = fieldObj.GetType().GetProperty("Elements")?.GetValue(fieldObj);
                    var getString = elements?.GetType().GetMethod("GetString", new[] { typeof(string) });
                    var vv = getString?.Invoke(elements, new object[] { "/V" })?.ToString();
                    if (!string.IsNullOrWhiteSpace(vv)) fieldValue = vv!;
                }
                catch { }
            }

            Console.WriteLine($"{i++}: Field '{fieldName}' => '{fieldValue}'");
        }
    }
}
