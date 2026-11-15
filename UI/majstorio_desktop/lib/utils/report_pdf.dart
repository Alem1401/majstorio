import 'dart:io';
import 'dart:typed_data';

import 'package:majstorio_desktop/utils/formatters.dart';
import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;

// Builds a report as a PDF document: a title, the chosen filters and one table.
// Returns the bytes of the file, which can be saved or sent to a printer.
Future<Uint8List> buildReportPdf({
  required String title,
  required String filters,
  required List<String> headers,
  required List<List<String>> rows,
}) async {
  final document = pw.Document(theme: _theme());

  document.addPage(
    // MultiPage continues the table on a new page when it does not fit on one.
    pw.MultiPage(
      pageFormat: PdfPageFormat.a4,
      build: (context) => [
        pw.Text(
          title,
          style: pw.TextStyle(fontSize: 20, fontWeight: pw.FontWeight.bold),
        ),
        pw.SizedBox(height: 4),
        pw.Text("Filters: $filters"),
        pw.Text("Generated: ${formatDateTime(DateTime.now().toUtc())}"),
        pw.SizedBox(height: 16),
        if (rows.isEmpty)
          pw.Text("There is no data for the selected filters.")
        else
          pw.TableHelper.fromTextArray(
            headers: headers,
            data: rows,
            headerStyle: pw.TextStyle(fontWeight: pw.FontWeight.bold),
            headerDecoration: const pw.BoxDecoration(color: PdfColors.grey300),
            cellAlignment: pw.Alignment.centerLeft,
          ),
      ],
    ),
  );

  return document.save();
}

// The built-in PDF font has no letters like č, ć, š, ž and đ, so the Windows Arial font is used.
pw.ThemeData _theme() {
  final regular = File(r'C:\Windows\Fonts\arial.ttf');
  final bold = File(r'C:\Windows\Fonts\arialbd.ttf');
  if (!regular.existsSync() || !bold.existsSync()) {
    return pw.ThemeData.base();
  }
  return pw.ThemeData.withFont(
    base: pw.Font.ttf(regular.readAsBytesSync().buffer.asByteData()),
    bold: pw.Font.ttf(bold.readAsBytesSync().buffer.asByteData()),
  );
}
